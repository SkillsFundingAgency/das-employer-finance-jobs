using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.EmployerFinance.Messages.Events;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.Activities;

public class RefreshPaymentDataCompletedEventActivities(
    ILogger<RefreshPaymentDataCompletedEventActivities> logger,
    IRefreshPaymentDataCompletedEventPublisher refreshPaymentDataCompletedEventPublisher)
{
    [Function(nameof(PublishRefreshPaymentDataCompletedEventActivity))]
    public async Task<PublishRefreshPaymentDataCompletedEventResult> PublishRefreshPaymentDataCompletedEventActivity([ActivityTrigger] PublishRefreshPaymentDataCompletedEventInput input)
    {
        if (input == null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        var completedEvent = new RefreshPaymentDataCompletedEvent
        {
            AccountId = input.AccountId,
            PeriodEnd = input.PeriodEnd,
            Created = DateTime.UtcNow,
            PaymentsProcessed = input.PaymentsProcessed
        };

        try
        {
            await refreshPaymentDataCompletedEventPublisher.Publish(completedEvent, input.CorrelationId);

            logger.LogInformation(
                "[CorrelationId: {CorrelationId}] Published RefreshPaymentDataCompletedEvent for AccountId {AccountId}, PeriodEnd {PeriodEnd}, PaymentsProcessed {PaymentsProcessed}.",
                input.CorrelationId,
                completedEvent.AccountId,
                completedEvent.PeriodEnd,
                completedEvent.PaymentsProcessed);

            return new PublishRefreshPaymentDataCompletedEventResult
            {
                Status = "Succeeded",
                Message = "RefreshPaymentDataCompletedEvent published."
            };
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "[CorrelationId: {CorrelationId}] Failed to publish RefreshPaymentDataCompletedEvent for AccountId {AccountId}, PeriodEnd {PeriodEnd}, PaymentsProcessed {PaymentsProcessed}.",
                input.CorrelationId,
                completedEvent.AccountId,
                completedEvent.PeriodEnd,
                completedEvent.PaymentsProcessed);

            throw;
        }
    }
}
