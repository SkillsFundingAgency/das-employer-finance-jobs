using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.Activities;

public class AccountPaymentsActivities(
    ILogger<AccountPaymentsActivities> logger,
    IAccountPaymentsImportService accountPaymentsImportService)
{
    [Function(nameof(ImportAccountPaymentsActivity))]
    public async Task<AccountPaymentsImportResult> ImportAccountPaymentsActivity([ActivityTrigger] ProcessAccountInput input)
    {
        if (input == null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        var request = new AccountPaymentsImportInput
        {
            AccountId = input.AccountId,
            PeriodEndRef = input.PeriodEndRef,
            CorrelationId = Guid.TryParse(input.CorrelationId, out var parsed) ? parsed : Guid.NewGuid(),
            IdempotencyKey = Guid.TryParse(input.IdempotencyKey, out var idempotency) ? idempotency : Guid.NewGuid()
        };

        logger.LogInformation(
                "[CorrelationId: {CorrelationId}] ImportAccountPaymentsActivity starting for AccountId: {AccountId} PeriodEnd: {PeriodEndRef}",
                request.CorrelationId,
                request.AccountId,
                request.PeriodEndRef);

        var result = await accountPaymentsImportService.ImportAccountPaymentsAsync(request, CancellationToken.None);

        logger.LogInformation("[CorrelationId: {CorrelationId}] ImportAccountPaymentsActivity completed for AccountId: {AccountId} PeriodEnd: {PeriodEndRef} Status: {Status}",
                request.CorrelationId,
                request.AccountId,
                request.PeriodEndRef,
                result.Status);

        return result;
    }

    [Function(nameof(ImportAccountExistingFinancePaymentIdsActivity))]
    public async Task<AccountExistingPaymentIdsImportResult> ImportAccountExistingFinancePaymentIdsActivity([ActivityTrigger] ProcessAccountInput input)
    {
        if (input == null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        var request = new AccountPaymentsImportInput
        {
            AccountId = input.AccountId,
            PeriodEndRef = input.PeriodEndRef,
            CorrelationId = Guid.TryParse(input.CorrelationId, out var parsed) ? parsed : Guid.NewGuid(),
            IdempotencyKey = Guid.TryParse(input.IdempotencyKey, out var idempotency) ? idempotency : Guid.NewGuid()
        };

        logger.LogInformation("[CorrelationId: {CorrelationId}] ImportAccountExistingFinancePaymentIdsActivity starting for AccountId: {AccountId}",
                request.CorrelationId,
                request.AccountId);

        var result = await accountPaymentsImportService.ImportAccountExistingPaymentIdsAsync(request.AccountId, request.CorrelationId.ToString());

        logger.LogInformation("[CorrelationId: {CorrelationId}] ImportAccountExistingFinancePaymentIdsActivity completed for AccountId: {AccountId} Status: {Status}",
                request.CorrelationId,
                request.AccountId,
                result.Status);

        return result;
    }
}
