using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Activities;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Models;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Requests;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Services;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.UnitTests.Helpers;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.UnitTests.Activities;

[TestFixture]
public class PersistLevyDeclarationsActivityTests
{
    private Mock<IFinanceApiClient<FinanceApiConfiguration>> _financeApi = null!;
    private Mock<ILogger<PersistLevyDeclarationsActivity>> _logger = null!;
    private Mock<IRetryService> _retryService = null!;
    private PersistLevyDeclarationsActivity _activity = null!;

    [SetUp]
    public void SetUp()
    {
        _financeApi = new Mock<IFinanceApiClient<FinanceApiConfiguration>>();
        _logger = new Mock<ILogger<PersistLevyDeclarationsActivity>>();
        _retryService = new Mock<IRetryService>();
        _retryService
            .Setup(x => x.ExecuteAsync(
                It.IsAny<Func<Task<ApiResponse<PersistLevyDeclarationsResponse>>>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>()))
            .Returns((Func<Task<ApiResponse<PersistLevyDeclarationsResponse>>> action, string _, string _, int _) => action());

        _activity = new PersistLevyDeclarationsActivity(_financeApi.Object, _retryService.Object, _logger.Object);
    }

    [Test]
    public async Task Run_Posts_NormalizedDeclarations_AndReturnsMetrics()
    {
        var input = CreateInput();
        IApiRequest? capturedRequest = null;
        var response = new PersistLevyDeclarationsResponse
        {
            DeclarationsPersisted = 2,
            DeclarationsSkipped = 0,
            TransactionsCreated = 2
        };

        _financeApi
            .Setup(x => x.PostWithResponseCode<PersistLevyDeclarationsResponse>(It.IsAny<IApiRequest>()))
            .Callback<IApiRequest>(request => capturedRequest = request)
            .ReturnsAsync(CreateResponse(response, HttpStatusCode.Created));

        var result = await _activity.Run(input);

        result.Success.Should().BeTrue();
        result.AccountId.Should().Be(input.AccountId);
        result.EmpRef.Should().Be(input.EmpRef);
        result.DeclarationsSubmitted.Should().Be(2);
        result.DeclarationsPersisted.Should().Be(2);
        result.DeclarationsSkipped.Should().Be(0);
        result.TransactionsCreated.Should().Be(2);

        capturedRequest.Should().BeOfType<PersistLevyDeclarationsRequest>();
        capturedRequest!.GetUrl.Should().Be("api/levy-declarations");
        var data = capturedRequest.Data.Should().BeOfType<PersistLevyDeclarationRequestData>().Subject;
        data.AccountId.Should().Be(input.AccountId);
        data.EmpRef.Should().Be(input.EmpRef);
        data.Declarations.Should().BeEquivalentTo(input.Declarations);

        _financeApi.Verify(
            x => x.PostWithResponseCode<PersistLevyDeclarationsResponse>(It.IsAny<IApiRequest>()),
            Times.Once);
        _logger.VerifyLogContains("Persisted levy declarations");
        _logger.VerifyLogContains("CorrelationId: corr-123");
        _logger.VerifyLogContains("AccountId 12345");
        _logger.VerifyLogContains("EmpRef 123/AB456");
        _logger.VerifyLogContains("DeclarationsPersisted 2");
        _logger.VerifyLogContains("TransactionsCreated 2");
    }

    [Test]
    public async Task Run_Retries_WhenFinanceApiReturnsTransientFailure()
    {
        var input = CreateInput();
        var retryDelay = new Mock<IRetryDelay>();
        retryDelay
            .Setup(x => x.DelayAsync(It.IsAny<TimeSpan>()))
            .Returns(Task.CompletedTask);
        var retryService = new RetryService(Mock.Of<ILogger<RetryService>>(), retryDelay.Object);
        var activity = new PersistLevyDeclarationsActivity(_financeApi.Object, retryService, _logger.Object);

        _financeApi
            .SetupSequence(x => x.PostWithResponseCode<PersistLevyDeclarationsResponse>(It.IsAny<IApiRequest>()))
            .ReturnsAsync(CreateResponse<PersistLevyDeclarationsResponse>(null!, HttpStatusCode.InternalServerError, "temporary failure"))
            .ReturnsAsync(CreateResponse(new PersistLevyDeclarationsResponse
            {
                DeclarationsPersisted = 2,
                DeclarationsSkipped = 0,
                TransactionsCreated = 2
            }, HttpStatusCode.OK));

        var result = await activity.Run(input);

        result.Success.Should().BeTrue();
        result.DeclarationsPersisted.Should().Be(2);
        _financeApi.Verify(
            x => x.PostWithResponseCode<PersistLevyDeclarationsResponse>(It.IsAny<IApiRequest>()),
            Times.Exactly(2));
        retryDelay.Verify(x => x.DelayAsync(TimeSpan.FromSeconds(2)), Times.Once);
    }

    [Test]
    public async Task Run_ReturnsSkippedAndZeroTransactions_WhenReplayPayloadAlreadyExists()
    {
        var input = CreateInput();

        _financeApi
            .SetupSequence(x => x.PostWithResponseCode<PersistLevyDeclarationsResponse>(It.IsAny<IApiRequest>()))
            .ReturnsAsync(CreateResponse(new PersistLevyDeclarationsResponse
            {
                DeclarationsPersisted = 2,
                DeclarationsSkipped = 0,
                TransactionsCreated = 2
            }, HttpStatusCode.Created))
            .ReturnsAsync(CreateResponse(new PersistLevyDeclarationsResponse
            {
                DeclarationsPersisted = 0,
                DeclarationsSkipped = 2,
                TransactionsCreated = 0
            }, HttpStatusCode.OK));

        var firstRun = await _activity.Run(input);
        var replay = await _activity.Run(input);

        firstRun.TransactionsCreated.Should().Be(2);
        replay.DeclarationsPersisted.Should().Be(0);
        replay.DeclarationsSkipped.Should().Be(2);
        replay.TransactionsCreated.Should().Be(0);
        replay.Success.Should().BeTrue();

        _financeApi.Verify(
            x => x.PostWithResponseCode<PersistLevyDeclarationsResponse>(It.IsAny<IApiRequest>()),
            Times.Exactly(2));
    }

    [Test]
    public async Task Run_DoesNotCallFinanceApi_WhenNoDeclarationsRemainAfterNormalization()
    {
        var input = CreateInput();
        input.SourceDeclarationCount = 4;
        input.Declarations = [];

        var result = await _activity.Run(input);

        result.Success.Should().BeTrue();
        result.DeclarationsSubmitted.Should().Be(0);
        result.DeclarationsSkipped.Should().Be(4);
        result.Message.Should().Be("No levy declarations to persist.");

        _financeApi.Verify(
            x => x.PostWithResponseCode<PersistLevyDeclarationsResponse>(It.IsAny<IApiRequest>()),
            Times.Never);
        _retryService.Verify(
            x => x.ExecuteAsync(
                It.IsAny<Func<Task<ApiResponse<PersistLevyDeclarationsResponse>>>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>()),
            Times.Never);
    }

    [Test]
    public async Task Run_Throws_WhenFinanceApiReturnsNonTransientFailure()
    {
        var input = CreateInput();

        _financeApi
            .Setup(x => x.PostWithResponseCode<PersistLevyDeclarationsResponse>(It.IsAny<IApiRequest>()))
            .ReturnsAsync(CreateResponse<PersistLevyDeclarationsResponse>(null!, HttpStatusCode.BadRequest, "bad request"));

        Func<Task> act = async () => await _activity.Run(input);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Failed to persist levy declarations*BadRequest*bad request*");

        _financeApi.Verify(
            x => x.PostWithResponseCode<PersistLevyDeclarationsResponse>(It.IsAny<IApiRequest>()),
            Times.Once);
    }

    [Test]
    public async Task Run_Throws_WhenInputIsNull()
    {
        Func<Task> act = async () => await _activity.Run(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    private static NormalizeLevyDeclarationsResult CreateInput()
    {
        return new NormalizeLevyDeclarationsResult
        {
            CorrelationId = "corr-123",
            AccountId = 12345,
            EmpRef = "123/AB456",
            SourceDeclarationCount = 2,
            Declarations =
            [
                new NormalizedLevyDeclaration
                {
                    Id = "declaration-1",
                    LevyDueYtd = 1250.45m,
                    SubmissionDate = new DateTime(2026, 4, 1),
                    SubmissionType = "FPS",
                    LevyAllowanceForFullYear = 15000,
                    PayrollYear = "25-26",
                    PayrollMonth = 1,
                    SubmissionId = 1001
                },
                new NormalizedLevyDeclaration
                {
                    Id = "declaration-2",
                    LevyDueYtd = 1750.45m,
                    SubmissionDate = new DateTime(2026, 4, 2),
                    SubmissionType = "EPS",
                    LevyAllowanceForFullYear = 15000,
                    PayrollYear = "25-26",
                    PayrollMonth = 2,
                    EndOfYearAdjustment = true,
                    EndOfYearAdjustmentAmount = 50,
                    SubmissionId = 1002
                }
            ]
        };
    }

    private static ApiResponse<T> CreateResponse<T>(T body, HttpStatusCode statusCode, string error = "")
    {
        return new ApiResponse<T>(body, statusCode, error, new Dictionary<string, IEnumerable<string>>());
    }
}
