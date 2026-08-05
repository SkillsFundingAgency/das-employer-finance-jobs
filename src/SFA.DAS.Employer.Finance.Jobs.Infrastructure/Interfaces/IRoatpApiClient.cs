using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

public interface IRoatpApiClient
{
    Task<ProviderDetails?> GetProvider(long ukprn);
}
