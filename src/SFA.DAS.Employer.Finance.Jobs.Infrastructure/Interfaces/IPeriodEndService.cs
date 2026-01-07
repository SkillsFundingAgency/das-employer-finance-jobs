using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

public interface IPeriodEndService
{
    Task<List<PeriodEnd>> GetNewPeriodEndsAsync(string correlationId);

    Task<PeriodEnd> CreatePeriodEndAsync(PeriodEnd periodEnd, string correlationId);
}