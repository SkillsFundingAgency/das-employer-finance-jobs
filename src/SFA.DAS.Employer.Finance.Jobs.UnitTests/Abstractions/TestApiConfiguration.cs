using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Abstractions;
public class TestApiConfiguration : IApiConfiguration
{
    public string Url { get; set; }
    public string Identifier { get; set; }
}
