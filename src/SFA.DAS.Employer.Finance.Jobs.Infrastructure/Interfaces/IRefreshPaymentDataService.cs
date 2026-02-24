using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models.RefreshPayments;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

public interface IRefreshPaymentDataService
{
    Task<RefreshPaymentDataResult> RefreshPaymentData(RefreshPaymentDataInput input);
}