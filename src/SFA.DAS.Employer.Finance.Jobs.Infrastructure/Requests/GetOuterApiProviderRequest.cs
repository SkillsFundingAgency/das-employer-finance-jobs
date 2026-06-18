using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;

public class GetOuterApiProviderRequest(long ukprn) : IApiRequest
{
    public string GetUrl => $"providers/{ukprn}";
    public object Data => null!;
}
