using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

public interface IEmployerFinanceOuterApiClient
{
    Task<ProviderDetails?> GetProvider(long ukprn);
    Task<StandardsResponse?> GetStandards();
    Task<FrameworksResponse?> GetFrameworks();
}
