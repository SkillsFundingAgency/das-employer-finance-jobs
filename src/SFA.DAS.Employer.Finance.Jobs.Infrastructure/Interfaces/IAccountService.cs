using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

public interface IAccountService
{
    Task<List<string>> GetAccountsAsync(GetAccountsRequest request);
}
