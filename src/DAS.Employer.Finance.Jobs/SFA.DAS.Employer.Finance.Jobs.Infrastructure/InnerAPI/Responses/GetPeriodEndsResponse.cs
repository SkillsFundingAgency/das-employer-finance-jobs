using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.InnerAPI.Responses;

public class GetPeriodEndsResponse
{
    public List<PeriodEnd> PeriodEnds { get; set; } = new List<PeriodEnd>();
}