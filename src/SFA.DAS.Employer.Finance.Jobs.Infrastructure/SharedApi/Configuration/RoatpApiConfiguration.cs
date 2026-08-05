using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;

public class RoatpApiConfiguration : IInternalApiConfiguration
{
    public string Url { get; set; } = string.Empty;
    public string Identifier { get; set; } = string.Empty;
}
