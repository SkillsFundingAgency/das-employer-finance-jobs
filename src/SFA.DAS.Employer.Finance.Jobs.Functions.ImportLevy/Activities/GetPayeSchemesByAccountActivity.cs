using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;


namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Activities;

public class GetPayeSchemesByAccountActivity(
    IFinanceApiClient<FinanceApiConfiguration> financeApi,
    ILogger<GetPayeSchemesByAccountActivity> logger)
{
    [Function("GetPayeSchemesByAccountActivity")]
    public async Task<List<PayeScheme>> Run([ActivityTrigger] GetPayeSchemesByAccountActivityRequest request)
    {
        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] Retrieving PAYE schemes for account {AccountId}",
            request.CorrelationId,
            request.AccountId);

        var response = await RetryAsync(
            () => financeApi.GetWithResponseCode<List<PayeScheme>>(
                new GetPayeSchemesByAccountRequest(request.AccountId, request.Source)),
            request.CorrelationId);

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

    private async Task<T> RetryAsync<T>(Func<Task<T>> action, string correlationId, int retries = 3)
    {
        var delay = TimeSpan.FromSeconds(2);

        for (var attempt = 1; attempt <= retries; attempt++)
        {
            try
            {
                return await action();
            }
            catch (Exception ex) when (attempt < retries)
            {
                logger.LogWarning(
                    ex,
                    "[CorrelationId: {CorrelationId}] [Retry {Attempt}] Temporary error calling Finance API, retrying...",
                    correlationId,
                    attempt);

                await Task.Delay(delay);
                delay *= 2;
            }
        }

        return await action();
    }
}
