using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Extensions;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.EmployerFinance.Messages.Events;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ExpireFunds.Activities;

public class AccountFundsExpiredEventActivities(
    IAccountFundsExpiredEventPublisher publisher,
    ILogger<AccountFundsExpiredEventActivities> logger)
{
    [Function(nameof(PublishAccountFundsExpiredEventActivity))]
    public async Task<PublishAccountFundsExpiredEventResult> PublishAccountFundsExpiredEventActivity(
        [ActivityTrigger] PublishAccountFundsExpiredEventInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var accountFundsExpiredEvent = new AccountFundsExpiredEvent
        {
            AccountId = input.AccountId,
            Created = input.Created
        };

        try
        {
            await publisher.Publish(
                accountFundsExpiredEvent,
                input.CorrelationId,
                input.MessageId);

            logger.LogInformation(
                "[CorrelationId: {CorrelationId}] Published AccountFundsExpiredEvent for AccountId {AccountId}, MessageId {MessageId}.",
                input.CorrelationId,
                input.AccountId,
                input.MessageId);

            return new PublishAccountFundsExpiredEventResult
            {
                AccountId = input.AccountId,
                Published = true
            };
        }
        catch (Exception exception) when (AccountFundsExpiredEventTransientErrorDetector.IsTransient(exception))
        {
            logger.LogWarning(
                exception,
                "[CorrelationId: {CorrelationId}] Failed to publish AccountFundsExpiredEvent with a transient error for AccountId {AccountId}, MessageId {MessageId}. Propagating for Durable Functions retry handling.",
                input.CorrelationId,
                input.AccountId,
                input.MessageId);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "[CorrelationId: {CorrelationId}] Failed to publish AccountFundsExpiredEvent for AccountId {AccountId}, MessageId {MessageId}.",
                input.CorrelationId,
                input.AccountId,
                input.MessageId);

            return new PublishAccountFundsExpiredEventResult
            {
                AccountId = input.AccountId,
                Published = false,
                ErrorMessage = exception.Message
            };
        }
    }
}
