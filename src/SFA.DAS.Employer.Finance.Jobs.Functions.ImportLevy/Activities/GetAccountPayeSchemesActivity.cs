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

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] Retrieving PAYE schemes for account {AccountId}",
            input.CorrelationId,
            input.AccountId);

        var request = new GetAccountPayeSchemesRequest
        {
            AccountId = input.AccountId,
            CorrelationId = ActivityExecutionHelper.ParseCorrelationIdOrNew(input.CorrelationId)
        };

        var payeSchemes = await ActivityExecutionHelper.RetryAsync(
            () => accountService.GetPayeSchemesAsync(request),
            logger,
            input.CorrelationId,
            "[CorrelationId: {CorrelationId}] [Retry {Attempt}] Temporary error retrieving PAYE schemes, retrying...",
            ex => new InvalidOperationException(
                $"[CorrelationId: {input.CorrelationId}] Failed to retrieve PAYE schemes for account {input.AccountId} after 3 attempts.",
                ex)) ?? [];

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] Retrieved {Count} PAYE schemes for account {AccountId}",
            input.CorrelationId,
            payeSchemes.Count,
            input.AccountId);

        return payeSchemes;
    }
}
