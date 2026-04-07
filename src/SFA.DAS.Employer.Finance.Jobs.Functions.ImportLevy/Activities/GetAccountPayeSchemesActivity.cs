using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Activities;

public class GetAccountPayeSchemesActivity(
    IAccountService accountService,
    ILogger<GetAccountPayeSchemesActivity> logger)
{
    [Function("GetAccountPayeSchemesActivity")]
    public async Task<List<PayeScheme>> Run([ActivityTrigger] GetAccountPayeSchemesActivityInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var requestCorrelationId = Guid.TryParse(input.CorrelationId, out var parsedCorrelationId)
            ? parsedCorrelationId
            : Guid.NewGuid();

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] Retrieving PAYE schemes for account {AccountId}",
            input.CorrelationId,
            input.AccountId);

        var request = new GetAccountPayeSchemesRequest
        {
            AccountId = input.AccountId,
            CorrelationId = requestCorrelationId
        };

        var payeSchemes = await RetryAsync(
            () => accountService.GetPayeSchemesAsync(request),
            input.CorrelationId) ?? [];

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] Retrieved {Count} PAYE schemes for account {AccountId}",
            input.CorrelationId,
            payeSchemes.Count,
            input.AccountId);

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
                    "[CorrelationId: {CorrelationId}] [Retry {Attempt}] Temporary error retrieving PAYE schemes, retrying...",
                    correlationId,
                    attempt);

                await Task.Delay(delay);
                delay *= 2;
            }
        }

        return await action();
    }
}
