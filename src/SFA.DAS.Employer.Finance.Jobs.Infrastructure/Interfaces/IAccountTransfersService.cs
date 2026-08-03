using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

public interface IAccountTransfersService
{
    Task<RefreshAccountTransfersResult> RefreshAccountTransfers(RefreshAccountTransfersInput input);
}
