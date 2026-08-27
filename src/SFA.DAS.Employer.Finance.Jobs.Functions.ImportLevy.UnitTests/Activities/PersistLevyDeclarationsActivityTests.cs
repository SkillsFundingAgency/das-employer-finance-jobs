using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Activities;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Models;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Requests;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Services;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.UnitTests.Helpers;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Extensions;
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
                It.IsAny<Func<Exception, bool>>(),
                It.IsAny<int>()))
            .Returns((Func<Task<ApiResponse<PersistLevyDeclarationsResponse>>> action, string _, string _, Func<Exception, bool> _, int _) => action());

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
        data.CorrelationId.Should().Be(input.CorrelationId);
        data.AccountId.Should().Be(input.AccountId);
        data.EmpRef.Should().Be(input.EmpRef);
        data.Declarations.Should().BeEquivalentTo(input.Declarations);
        data.GenerateTransactions.Should().BeTrue();

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
    public async Task Run_Retries_WhenFinanceApiThrowsTransientFailure()
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
            .ThrowsAsync(new HttpRequestContentException(
                "temporary failure",
                HttpStatusCode.InternalServerError,
                "temporary failure"))
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
            .ReturnsAsync(CreateResponse<PersistLevyDeclarationsResponse>(
                null!,
                HttpStatusCode.InternalServerError,
                "temporary failure"))
            .ReturnsAsync(CreateResponse(new PersistLevyDeclarationsResponse
            {
                DeclarationsPersisted = 2,
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
    public async Task Run_SendsIdenticalPayloadAndReturnsNoNewTransactions_WhenReplayPayloadAlreadyExists()
    {
        var input = CreateInput();
        var persistedDeclarationIds = new HashSet<string>();
        var serializedPayloads = new List<string>();

        _financeApi
            .Setup(x => x.PostWithResponseCode<PersistLevyDeclarationsResponse>(It.IsAny<IApiRequest>()))
            .ReturnsAsync((IApiRequest request) =>
            {
                var data = request.Data.Should().BeOfType<PersistLevyDeclarationRequestData>().Subject;
                serializedPayloads.Add(JsonSerializer.Serialize(data));

                var persisted = data.Declarations.Count(x => persistedDeclarationIds.Add(x.Id));
                return CreateResponse(new PersistLevyDeclarationsResponse
                {
                    DeclarationsPersisted = persisted,
                    DeclarationsSkipped = data.Declarations.Count - persisted,
                    TransactionsCreated = persisted
                }, HttpStatusCode.OK);
            });

        var firstRun = await _activity.Run(input);
        var replay = await _activity.Run(input);

        firstRun.TransactionsCreated.Should().Be(2);
        replay.DeclarationsPersisted.Should().Be(0);
        replay.DeclarationsSkipped.Should().Be(2);
        replay.TransactionsCreated.Should().Be(0);
        replay.Success.Should().BeTrue();
        serializedPayloads.Should().HaveCount(2);
        serializedPayloads[1].Should().Be(serializedPayloads[0]);

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
                It.IsAny<Func<Exception, bool>>(),
                It.IsAny<int>()),
            Times.Never);
        _logger.VerifyLogContains("DeclarationsSubmitted 0");
        _logger.VerifyLogContains("DeclarationsPersisted 0");
        _logger.VerifyLogContains("DeclarationsSkipped 4");
        _logger.VerifyLogContains("TransactionsCreated 0");
    }

    [Test]
    public async Task Run_DoesNotRetry_WhenFinanceApiThrowsNonTransientFailure()
    {
        var input = CreateInput();
        var retryDelay = new Mock<IRetryDelay>();
        var retryService = new RetryService(Mock.Of<ILogger<RetryService>>(), retryDelay.Object);
        var activity = new PersistLevyDeclarationsActivity(_financeApi.Object, retryService, _logger.Object);

        _financeApi
            .Setup(x => x.PostWithResponseCode<PersistLevyDeclarationsResponse>(It.IsAny<IApiRequest>()))
            .ThrowsAsync(new HttpRequestContentException("bad request", HttpStatusCode.BadRequest, "bad request"));

        Func<Task> act = async () => await activity.Run(input);

        var exception = await act.Should().ThrowAsync<HttpRequestContentException>();
        exception.Which.Message.Should().Be("bad request. Content: bad request");
        exception.Which.ErrorContent.Should().Be("bad request");
        exception.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        _financeApi.Verify(
            x => x.PostWithResponseCode<PersistLevyDeclarationsResponse>(It.IsAny<IApiRequest>()),
            Times.Once);
        retryDelay.Verify(x => x.DelayAsync(It.IsAny<TimeSpan>()), Times.Never);
        _logger.VerifyLogContains("Failed to persist levy declarations");
    }

    [Test]
    public async Task Run_DoesNotRetry_WhenFinanceApiReturnsNonTransientFailure()
    {
        var input = CreateInput();
        var retryDelay = new Mock<IRetryDelay>();
        var retryService = new RetryService(Mock.Of<ILogger<RetryService>>(), retryDelay.Object);
        var activity = new PersistLevyDeclarationsActivity(_financeApi.Object, retryService, _logger.Object);

        _financeApi
            .Setup(x => x.PostWithResponseCode<PersistLevyDeclarationsResponse>(It.IsAny<IApiRequest>()))
            .ReturnsAsync(CreateResponse<PersistLevyDeclarationsResponse>(
                null!,
                HttpStatusCode.BadRequest,
                "bad request"));

        Func<Task> act = async () => await activity.Run(input);

        await act.Should().ThrowAsync<HttpRequestContentException>()
            .WithMessage("*StatusCode: BadRequest*");

        _financeApi.Verify(
            x => x.PostWithResponseCode<PersistLevyDeclarationsResponse>(It.IsAny<IApiRequest>()),
            Times.Once);
        retryDelay.Verify(x => x.DelayAsync(It.IsAny<TimeSpan>()), Times.Never);
        _logger.VerifyLogContains("Failed to persist levy declarations");
    }

    [Test]
    public async Task Run_ThrowsContractFailure_WhenFinanceApiReturnsSuccessWithoutMetrics()
    {
        var input = CreateInput();

        _financeApi
            .Setup(x => x.PostWithResponseCode<PersistLevyDeclarationsResponse>(It.IsAny<IApiRequest>()))
            .ReturnsAsync(CreateResponse<PersistLevyDeclarationsResponse>(null!, HttpStatusCode.NoContent));

        Func<Task> act = async () => await _activity.Run(input);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*successful response without result metrics*");
        _logger.VerifyLogContains("Failed to persist levy declarations");
    }

    [Test]
    public async Task Run_ThrowsContractFailure_WhenFinanceApiReturnsNoResponse()
    {
        var input = CreateInput();

        _financeApi
            .Setup(x => x.PostWithResponseCode<PersistLevyDeclarationsResponse>(It.IsAny<IApiRequest>()))
            .ReturnsAsync((ApiResponse<PersistLevyDeclarationsResponse>)null!);

        Func<Task> act = async () => await _activity.Run(input);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*returned no response*");
        _logger.VerifyLogContains("Failed to persist levy declarations");
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
                    Id = "1001",
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
                    Id = "1002",
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
