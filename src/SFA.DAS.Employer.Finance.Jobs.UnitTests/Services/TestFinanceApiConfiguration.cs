using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Services
{
    public class TestFinanceApiConfiguration : IInternalApiConfiguration
    {
        public string Url { get; set; }
        public string Identifier { get; set; }
    }
}