using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Options;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests.FundingProjection;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses.FundingProjection;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.UnitTests.Helpers;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.SharedApi;

public class WhenCallingTheEmployerFinanceJobsOuterApi
{
    // Gateway base urls include the api's path segment, which must be preserved
    private const string BaseUrl = "https://gateway.example.test/outer-api/";

    private static IEnumerable<TestCaseData> FundingProjectionRequests()
    {
        var jobId = Guid.NewGuid();
        yield return new TestCaseData(
                new ImportCommittedLearnersRequest(new ImportCommittedLearnersRequestData(12345, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc))),
                $"{BaseUrl}api/apprenticeship/import-learners")
            .SetName("ImportCommittedLearnersRequest");
        yield return new TestCaseData(
                new PostJobRequest(jobId, new PostJobRequestData
                {
                    AccountId = 12345,
                    JobState = "Completed",
                    LastSuccessfulImportDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    LastAttemptedDate = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                    LastAttemptSuccessful = true,
                    TotalRecordsLastRun = 1,
                    FailedRecordsLastRun = 0
                }),
                $"{BaseUrl}api/jobs/{jobId}")
            .SetName("PostJobRequest");
    }

    [TestCaseSource(nameof(FundingProjectionRequests))]
    public async Task Then_The_Request_Is_Sent_Under_The_Apim_Base_Path(IApiRequest request, string expectedUrl)
    {
        var handler = new StubHttpMessageHandler(_ => StubHttpMessageHandler.JsonResponse("{}"));

        await CreateClient(handler).PostWithResponseCodeAsync<object>(request);

        handler.Requests.Single().RequestUri!.ToString().Should().Be(expectedUrl);
    }

    [Test]
    public async Task Then_An_Empty_Success_Body_Does_Not_Throw()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));

        var response = await CreateClient(handler).PostWithResponseCodeAsync<ImportCommittedLearnersResponse>(
            new ImportCommittedLearnersRequest(new ImportCommittedLearnersRequestData(12345, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc))));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Body.Should().BeNull();
    }

    private static OuterApiClient<EmployerFinanceJobsOuterApiConfiguration> CreateClient(StubHttpMessageHandler handler) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) },
            Options.Create(new EmployerFinanceJobsOuterApiConfiguration { BaseUrl = BaseUrl, Key = "key" }));
}
