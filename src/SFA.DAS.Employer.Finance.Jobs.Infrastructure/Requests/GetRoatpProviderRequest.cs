using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;

public class GetRoatpProviderRequest(long ukprn) : IApiRequest
{
    public string GetUrl => $"api/providers/{ukprn}";
    public object Data => null!;
}
