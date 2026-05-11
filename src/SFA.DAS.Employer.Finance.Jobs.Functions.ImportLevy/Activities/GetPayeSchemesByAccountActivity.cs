using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Services;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;
using System.Net;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Activities;

public class GetPayeSchemesByAccountActivity(
    IFinanceApiClient<FinanceApiConfiguration> financeApi,
    IRetryService retryService,
    ILogger<GetPayeSchemesByAccountActivity> logger)
{
    [Function("GetPayeSchemesByAccountActivity")]
    public async Task<List<PayeScheme>> Run([ActivityTrigger] GetPayeSchemesByAccountActivityRequest request)
    {
        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] Retrieving PAYE schemes for account {AccountId}",
            request.CorrelationId,
            request.AccountId);

        var response = await retryService.ExecuteAsync(
            () => financeApi.GetWithResponseCode<List<PayeScheme>>(
                new GetPayeSchemesByAccountRequest(request.AccountId, request.Source)),
            request.CorrelationId,
            "Finance API");

        if (response == null || response.StatusCode != HttpStatusCode.OK)
        {
            throw new InvalidOperationException(
                $"[CorrelationId: {request.CorrelationId}] Failed to retrieve PAYE schemes for account {request.AccountId}");
        }

        var payeSchemes = response.Body ?? [];

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] Retrieved {Count} PAYE schemes for account {AccountId}",
            request.CorrelationId,
            payeSchemes.Count,
            request.AccountId);

        return payeSchemes;
    }
}
