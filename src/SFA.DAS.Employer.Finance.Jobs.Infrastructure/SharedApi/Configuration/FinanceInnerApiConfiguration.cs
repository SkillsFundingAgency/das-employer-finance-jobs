using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;
using System.Diagnostics.CodeAnalysis;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration
{
    [ExcludeFromCodeCoverage]
    public class FinanceInnerApiConfiguration : IInternalApiConfiguration
    {
        public string Url { get; set; } 
        public string Identifier { get; set; } 
    }
}
