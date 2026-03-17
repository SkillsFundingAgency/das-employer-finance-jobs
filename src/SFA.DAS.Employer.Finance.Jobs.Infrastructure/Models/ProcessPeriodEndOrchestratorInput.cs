namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public class ProcessPeriodEndOrchestratorInput
{
    public PeriodEnd PeriodEnd { get; set; }
    public int MaxConcurrentAccounts { get; set; }
}
