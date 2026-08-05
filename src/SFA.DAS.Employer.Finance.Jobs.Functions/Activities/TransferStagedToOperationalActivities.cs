using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.Activities;

public class TransferStagedToOperationalActivities(
    ILogger<TransferStagedToOperationalActivities> logger,
    ITransferStagedToOperationalService transferStagedToOperationalService)
{
    [Function(nameof(TransferStagedToOperationalActivity))]
    public async Task<TransferStagedToOperationalResult> TransferStagedToOperationalActivity([ActivityTrigger] TransferStagedToOperationalInput input)
    {
        if (input == null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] TransferStagedToOperationalActivity starting for AccountId: {AccountId} PeriodEnd: {PeriodEndRef}",
            input.CorrelationId,
            input.AccountId,
            input.PeriodEndRef);

        var result = await transferStagedToOperationalService.Process(input);

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] TransferStagedToOperationalActivity completed for AccountId: {AccountId} PeriodEnd: {PeriodEndRef}. Status: {Status}. Message: {Message}",
            input.CorrelationId,
            input.AccountId,
            input.PeriodEndRef,
            result.Status,
            result.Message);

        return result;
    }
}
