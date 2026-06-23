using SFA.DAS.EmployerFinance.Messages.Events;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

public interface IRefreshPaymentDataCompletedEventPublisher
{
    Task Publish(RefreshPaymentDataCompletedEvent refreshPaymentDataCompletedEvent, string correlationId, CancellationToken cancellationToken = default);
}
