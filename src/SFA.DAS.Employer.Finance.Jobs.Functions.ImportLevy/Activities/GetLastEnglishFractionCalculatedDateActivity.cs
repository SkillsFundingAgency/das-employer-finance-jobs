using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Models;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Services;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;
using System.Net;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Activities;

public class GetLastEnglishFractionCalculatedDateActivity(
    IFinanceApiClient<FinanceApiConfiguration> financeApi,
    IRetryService retryService,
    ILogger<GetLastEnglishFractionCalculatedDateActivity> logger)
{
    [Function("GetLastEnglishFractionCalculatedDateActivity")]
    public async Task<DateTime?> Run([ActivityTrigger] GetLastEnglishFractionCalculatedDateActivityRequest request)
    {
        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] Retrieving last stored english fraction calculation date for EmpRef {EmpRef}",
            request.CorrelationId,
            request.EmpRef);

        var response = await retryService.ExecuteAsync(
            () => financeApi.GetWithResponseCode<LastEnglishFractionCalculationDateResult>(
                new GetLastEnglishFractionCalculationDateRequest(request.EmpRef)),
            request.CorrelationId,
            "Finance API");

        if (response == null || response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw new InvalidOperationException(
                $"[CorrelationId: {request.CorrelationId}] Failed to retrieve english fraction calculation date for EmpRef {request.EmpRef}");
        }

        return response.Body?.DateCalculated;
    }
}
