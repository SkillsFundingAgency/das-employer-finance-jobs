using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.EmployerFinance.Messages.Events;
using NServiceBus;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;

public class RefreshPaymentDataCompletedEventPublisher(
    IMessageSession messageSession,
    ILogger<RefreshPaymentDataCompletedEventPublisher> logger) : IRefreshPaymentDataCompletedEventPublisher
{
    public async Task Publish(RefreshPaymentDataCompletedEvent refreshPaymentDataCompletedEvent, string correlationId, CancellationToken cancellationToken = default)
    {
        var publishOptions = new PublishOptions();
        publishOptions.SetHeader(Headers.CorrelationId, correlationId);

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] Publishing RefreshPaymentDataCompletedEvent through NServiceBus for AccountId {AccountId}, PeriodEnd {PeriodEnd}, PaymentsProcessed {PaymentsProcessed}.",
            correlationId,
            refreshPaymentDataCompletedEvent.AccountId,
            refreshPaymentDataCompletedEvent.PeriodEnd,
            refreshPaymentDataCompletedEvent.PaymentsProcessed);

        await messageSession.Publish(refreshPaymentDataCompletedEvent, publishOptions);
    }
}
