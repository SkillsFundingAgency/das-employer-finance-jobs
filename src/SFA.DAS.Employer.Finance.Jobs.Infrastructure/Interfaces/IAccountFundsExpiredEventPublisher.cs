using SFA.DAS.EmployerFinance.Messages.Events;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

public interface IAccountFundsExpiredEventPublisher
{
    Task Publish(
        AccountFundsExpiredEvent accountFundsExpiredEvent,
        string correlationId,
        string messageId,
        CancellationToken cancellationToken = default);
}
