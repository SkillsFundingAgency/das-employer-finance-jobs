using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.Activities;

public class AccountPaymentPageStagingActivities(
    ILogger<AccountPaymentPageStagingActivities> logger,
    IAccountPaymentPageStagingService accountPaymentPageStagingService)
{
    [Function(nameof(StageAccountPaymentsPageActivity))]
    public async Task<StageAccountPaymentsPageResult> StageAccountPaymentsPageActivity(
        [ActivityTrigger] StageAccountPaymentsPageInput input)
    {
        if (input == null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] StageAccountPaymentsPageActivity starting for AccountId {AccountId} PeriodEnd {PeriodEndRef} Page {PageNumber}",
            input.CorrelationId,
            input.AccountId,
            input.PeriodEndRef,
            input.PageNumber);

        var result = await accountPaymentPageStagingService.StageAccountPaymentsPageAsync(input, CancellationToken.None);

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] StageAccountPaymentsPageActivity completed for AccountId {AccountId} PeriodEnd {PeriodEndRef} Page {PageNumber}/{TotalPages}. ItemCount: {ItemCount}. PaymentsCreated: {PaymentsCreated}. Status: {Status}",
            input.CorrelationId,
            input.AccountId,
            input.PeriodEndRef,
            result.PageNumber,
            result.TotalPages,
            result.ItemCount,
            result.PaymentsCreated,
            result.Status);

        return result;
    }
}
