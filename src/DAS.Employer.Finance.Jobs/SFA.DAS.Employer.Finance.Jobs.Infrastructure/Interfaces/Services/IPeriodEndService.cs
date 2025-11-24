using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces.Services;

public interface IPeriodEndService
{
    Task<List<PeriodEnd>> GetNewPeriodEndsAsync(string correlationId);
}
