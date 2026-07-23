using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.Activities;

public class AccountTransferActivities(
    ILogger<AccountTransferActivities> logger,
    IAccountTransfersService accountTransfersService)
{
    [Function(nameof(RefreshAccountTransfersActivity))]
    public async Task<RefreshAccountTransfersResult> RefreshAccountTransfersActivity([ActivityTrigger] RefreshAccountTransfersInput input)
    {
        if (input == null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] RefreshAccountTransfersActivity starting for AccountId: {AccountId} PeriodEnd: {PeriodEndRef}",
            input.CorrelationId,
            input.AccountId,
            input.PeriodEndRef);

        var result = await accountTransfersService.RefreshAccountTransfers(input);

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] RefreshAccountTransfersActivity completed for AccountId: {AccountId} PeriodEnd: {PeriodEndRef}. Status: {Status}. TransfersProcessed: {TransfersProcessed}. Message: {Message}",
            input.CorrelationId,
            input.AccountId,
            input.PeriodEndRef,
            result.Status,
            result.TransfersProcessed,
            result.Message);

        return result;
    }
}
