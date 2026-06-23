using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.Activities;

public class RefreshPaymentDataActivities(
    ILogger<RefreshPaymentDataActivities> logger,
    IRefreshPaymentDataService refreshPaymentDataService)
{
    [Function(nameof(RefreshPaymentDataActivity))]
    public async Task<RefreshPaymentDataActivityResult> RefreshPaymentDataActivity([ActivityTrigger] RefreshPaymentDataInput input)
    {
        if (input == null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        var payments = input.Payments ?? new List<SFA.DAS.Provider.Events.Api.Types.Payment>();
        var existingPaymentIds = input.PaymentIds ?? new List<string>();

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] RefreshPaymentDataActivity starting for AccountId: {AccountId}. Payments: {PaymentCount}. ExistingPaymentIds: {ExistingPaymentIdCount}.",
            input.CorrelationId,
            input.AccountId,
            payments.Count,
            existingPaymentIds.Count);

        var filteredPayments = refreshPaymentDataService.FilterPayments(payments, existingPaymentIds, input.AccountId, input.CorrelationId);
        var filteredPaymentDetails = payments
            .Where(payment => Guid.TryParse(payment.Id, out var paymentId) && filteredPayments.Any(filtered => filtered.PaymentId == paymentId))
            .ToList();

        if (!filteredPayments.Any())
        {
            return new RefreshPaymentDataActivityResult
            {
                PaymentsCreated = 0,
                PaymentDetails = Array.Empty<SFA.DAS.Provider.Events.Api.Types.Payment>(),
                Status = "Succeeded",
                Message = "No new payments to post into staging."
            };
        }

        var result = await refreshPaymentDataService.PostPaymentsToStaging(filteredPayments, input.CorrelationId);

        logger.LogInformation("[CorrelationId: {CorrelationId}] RefreshPaymentDataActivity completed for AccountId: {AccountId} Status: {Status} Message: {Message}",
                input.CorrelationId,
                input.AccountId,
                result.Status,
                result.Message);

        return new RefreshPaymentDataActivityResult
        {
            PaymentsCreated = result.PaymentsCreated,
            PaymentDetails = result.Status == "Succeeded" ? filteredPaymentDetails : Array.Empty<SFA.DAS.Provider.Events.Api.Types.Payment>(),
            Status = result.Status,
            Message = result.Status == "Succeeded"
                ? $"Filtered payments {filteredPayments.Count} are posted to staging."
                : result.Message
        };
    }
}
