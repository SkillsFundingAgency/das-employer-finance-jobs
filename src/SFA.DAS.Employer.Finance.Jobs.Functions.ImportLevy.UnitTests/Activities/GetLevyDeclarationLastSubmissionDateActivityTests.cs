using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Activities;
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
    private Mock<ILogger<GetLevyDeclarationLastSubmissionDateActivity>> _logger = null!;
    private GetLevyDeclarationLastSubmissionDateActivity _activity = null!;

    [SetUp]
    public void SetUp()
    {
        _financeApi = new Mock<IFinanceApiClient<FinanceApiConfiguration>>();
        _logger = new Mock<ILogger<GetLevyDeclarationLastSubmissionDateActivity>>();
        _activity = new GetLevyDeclarationLastSubmissionDateActivity(_financeApi.Object, _logger.Object);
    }

    [Test]
    public async Task Run_Returns_PayeScheme_With_LastSubmissionDate()
    {
        var request = new GetLevyDeclarationLastSubmissionDateActivityRequest("123/AB12345", "corr-123");
        var submissionDate = new DateTime(2026, 4, 1);

        _financeApi
            .Setup(x => x.GetWithResponseCode<LastSubmissionDateResult>(It.IsAny<GetLevyDeclarationLastSubmissionDateRequest>()))
            .ReturnsAsync(CreateResponse(new LastSubmissionDateResult { LastSumissionDate = submissionDate }));

        var result = await _activity.Run(request);

        result.Reference.Should().Be("123/AB12345");
        result.LastSubmissionDate.Should().Be(submissionDate);
        _financeApi.Verify(
            x => x.GetWithResponseCode<LastSubmissionDateResult>(It.Is<GetLevyDeclarationLastSubmissionDateRequest>(r =>
                r.GetUrl == "api/levy-declarations/123%2fAB12345/last-submission-date")),
            Times.Once);
    }

    [Test]
    public async Task Run_Retries_And_Succeeds_After_Transient_Failure()
    {
        var request = new GetLevyDeclarationLastSubmissionDateActivityRequest("123/AB12345", "corr-123");

        _financeApi
            .SetupSequence(x => x.GetWithResponseCode<LastSubmissionDateResult>(It.IsAny<GetLevyDeclarationLastSubmissionDateRequest>()))
            .ThrowsAsync(new Exception("temporary failure"))
            .ReturnsAsync(CreateResponse(new LastSubmissionDateResult { LastSumissionDate = new DateTime(2026, 4, 1) }));

        var result = await _activity.Run(request);

        result.Reference.Should().Be("123/AB12345");
        _financeApi.Verify(
            x => x.GetWithResponseCode<LastSubmissionDateResult>(It.IsAny<GetLevyDeclarationLastSubmissionDateRequest>()),
            Times.Exactly(2));
    }

    [Test]
    public void Run_Throws_When_All_Retries_Are_Exhausted()
    {
        var request = new GetLevyDeclarationLastSubmissionDateActivityRequest("123/AB12345", "corr-123");

        _financeApi
            .Setup(x => x.GetWithResponseCode<LastSubmissionDateResult>(It.IsAny<GetLevyDeclarationLastSubmissionDateRequest>()))
            .ThrowsAsync(new Exception("still failing"));

        Func<Task> act = async () => await _activity.Run(request);

        act.Should().ThrowAsync<Exception>().Wait();
        _financeApi.Verify(
            x => x.GetWithResponseCode<LastSubmissionDateResult>(It.IsAny<GetLevyDeclarationLastSubmissionDateRequest>()),
            Times.Exactly(3));
    }

    private static ApiResponse<LastSubmissionDateResult> CreateResponse(LastSubmissionDateResult body)
    {
        return new ApiResponse<LastSubmissionDateResult>(body, HttpStatusCode.OK, string.Empty, new Dictionary<string, IEnumerable<string>>());
    }
}
