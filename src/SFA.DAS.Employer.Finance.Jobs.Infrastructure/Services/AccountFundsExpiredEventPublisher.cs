using Microsoft.Extensions.Logging;
using NServiceBus;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.EmployerFinance.Messages.Events;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;

public class AccountFundsExpiredEventPublisher(
    IMessageSession messageSession,
    ILogger<AccountFundsExpiredEventPublisher> logger) : IAccountFundsExpiredEventPublisher
{
    public async Task Publish(
        AccountFundsExpiredEvent accountFundsExpiredEvent,
        string correlationId,
        string messageId,
        CancellationToken cancellationToken = default)
    {
        var publishOptions = new PublishOptions();
        publishOptions.SetHeader(Headers.CorrelationId, correlationId);
        publishOptions.SetMessageId(messageId);

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] Publishing AccountFundsExpiredEvent through NServiceBus for AccountId {AccountId}, MessageId {MessageId}.",
            correlationId,
            accountFundsExpiredEvent.AccountId,
            messageId);

        await messageSession.Publish(accountFundsExpiredEvent, publishOptions, cancellationToken);
    }
}
