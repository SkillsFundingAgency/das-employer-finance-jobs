using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Requests;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Services;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Activities;

public class GetExistingLevySubmissionIdsActivity(
    IFinanceApiClient<FinanceApiConfiguration> financeApi,
    IRetryService retryService,
    ILogger<GetExistingLevySubmissionIdsActivity> logger)
{
    [Function("GetExistingLevySubmissionIdsActivity")]
    public async Task<List<string>> Run([ActivityTrigger] GetExistingLevySubmissionIdsActivityRequest request)
    {
        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] Retrieving existing levy declaration submission ids for EmpRef {EmpRef}",
            request.CorrelationId,
            request.EmpRef);

        var response = await retryService.ExecuteAsync(
            () => financeApi.GetWithResponseCode<List<string>>(new GetExistingLevySubmissionIdsRequest(request.EmpRef)),
            request.CorrelationId,
            "Finance API");

        if (response == null || response.StatusCode == HttpStatusCode.NotFound)
        {
            return [];
        }

        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw new InvalidOperationException(
                $"[CorrelationId: {request.CorrelationId}] Failed to retrieve existing levy submission ids for EmpRef {request.EmpRef}");
        }

        return response.Body ?? [];
    }
}
