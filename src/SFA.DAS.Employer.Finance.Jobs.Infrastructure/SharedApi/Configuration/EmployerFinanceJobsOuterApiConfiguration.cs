using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;

public sealed record EmployerFinanceJobsOuterApiConfiguration : IOuterApiConfiguration
{
    public required string Key { get; set; }
    public required string BaseUrl { get; set; }
}