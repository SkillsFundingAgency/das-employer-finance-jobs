using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration
{
    public class TestProviderPaymentApiConfiguration : IInternalApiConfiguration
    {
        public string Url { get; set; } 
        public string Identifier { get; set; } 
    }
}
