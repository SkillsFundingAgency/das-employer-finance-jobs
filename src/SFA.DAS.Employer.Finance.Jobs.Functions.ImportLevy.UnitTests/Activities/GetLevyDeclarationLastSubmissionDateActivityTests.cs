using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Activities;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Services;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.UnitTests.Activities;

[TestFixture]
public class GetLevyDeclarationLastSubmissionDateActivityTests
{
    private Mock<IFinanceApiClient<FinanceApiConfiguration>> _financeApi = null!;
    private Mock<IRetryService> _retryService = null!;
    private Mock<ILogger<GetLevyDeclarationLastSubmissionDateActivity>> _logger = null!;
    private GetLevyDeclarationLastSubmissionDateActivity _activity = null!;

    [SetUp]
    public void SetUp()
    {
        _financeApi = new Mock<IFinanceApiClient<FinanceApiConfiguration>>();
        _retryService = new Mock<IRetryService>();
        _logger = new Mock<ILogger<GetLevyDeclarationLastSubmissionDateActivity>>();
        _retryService
            .Setup(x => x.ExecuteAsync(
                It.IsAny<Func<Task<ApiResponse<LastSubmissionDateResult>>>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>()))
            .Returns((Func<Task<ApiResponse<LastSubmissionDateResult>>> action, string _, string _, int _) => action());

        _activity = new GetLevyDeclarationLastSubmissionDateActivity(
            _financeApi.Object,
            _retryService.Object,
            _logger.Object);
    }

    [Test]
    public async Task Run_Returns_PayeScheme_With_LastSubmissionDate()
    {
        var request = new GetLevyDeclarationLastSubmissionDateActivityRequest("123/AB12345", "corr-123");
        var submissionDate = new DateTime(2026, 4, 1);

        _financeApi
            .Setup(x => x.GetWithResponseCode<LastSubmissionDateResult>(It.IsAny<GetLevyDeclarationLastSubmissionDateRequest>()))
            .ReturnsAsync(CreateResponse(new LastSubmissionDateResult { LastSubmissionDate = submissionDate }));

        var result = await _activity.Run(request);

        result.Reference.Should().Be("123/AB12345");
        result.LastSubmissionDate.Should().Be(submissionDate);
        _financeApi.Verify(
            x => x.GetWithResponseCode<LastSubmissionDateResult>(It.Is<GetLevyDeclarationLastSubmissionDateRequest>(r =>
                r.GetUrl == "api/paye-schemes/last-submission-date?empRef=123%2FAB12345")),
            Times.Once);
        _retryService.Verify(
            x => x.ExecuteAsync(
                It.IsAny<Func<Task<ApiResponse<LastSubmissionDateResult>>>>(),
                "corr-123",
                "Finance API",
                RetryService.DefaultRetries),
            Times.Once);
    }

    [Test]
    public async Task Run_Builds_Query_String_Url_For_At_Paye_Ref()
    {
        var request = new GetLevyDeclarationLastSubmissionDateActivityRequest("001/AC004317", "corr-123");

        _financeApi
            .Setup(x => x.GetWithResponseCode<LastSubmissionDateResult>(It.IsAny<GetLevyDeclarationLastSubmissionDateRequest>()))
            .ReturnsAsync(CreateResponse(new LastSubmissionDateResult { LastSubmissionDate = null }));

        await _activity.Run(request);

        _financeApi.Verify(
            x => x.GetWithResponseCode<LastSubmissionDateResult>(It.Is<GetLevyDeclarationLastSubmissionDateRequest>(r =>
                r.GetUrl == "api/paye-schemes/last-submission-date?empRef=001%2FAC004317")),
            Times.Once);
    }

    [Test]
    public void Run_Throws_WhenRetryServiceThrows()
    {
        var request = new GetLevyDeclarationLastSubmissionDateActivityRequest("123/AB12345", "corr-123");

        _retryService
            .Setup(x => x.ExecuteAsync(
                It.IsAny<Func<Task<ApiResponse<LastSubmissionDateResult>>>>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>()))
            .ThrowsAsync(new Exception("still failing"));

        Func<Task> act = async () => await _activity.Run(request);

        act.Should().ThrowAsync<Exception>().Wait();
    }

    private static ApiResponse<LastSubmissionDateResult> CreateResponse(LastSubmissionDateResult body)
    {
        return new ApiResponse<LastSubmissionDateResult>(
            body,
            HttpStatusCode.OK,
            string.Empty,
            new Dictionary<string, IEnumerable<string>>());
    }
}
