using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration
{
    public class FinanceApiConfiguration : IInternalApiConfiguration
    {
        public string Url { get; set; } 
        public string Identifier { get; set; } 
    }
}
