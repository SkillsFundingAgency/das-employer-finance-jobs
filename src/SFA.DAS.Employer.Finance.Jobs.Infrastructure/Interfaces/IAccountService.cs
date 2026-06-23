using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

public interface IAccountService
{
    Task<List<Accounts>> GetAccountsAsync(GetAccountsRequest request);
    Task<List<PayeScheme>> GetPayeSchemesAsync(GetAccountPayeSchemesRequest request);
}
