using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

public interface IAccountPaymentPageStagingService
{
    Task<StageAccountPaymentsPageResult> StageAccountPaymentsPageAsync(
        StageAccountPaymentsPageInput input,
        CancellationToken cancellationToken);
}
