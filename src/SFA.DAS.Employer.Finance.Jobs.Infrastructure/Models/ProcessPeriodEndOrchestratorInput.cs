namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public class ProcessPeriodEndOrchestratorInput
{
    public string CorrelationId { get; set; }
    public PeriodEnd PeriodEnd { get; set; }
    public int MaxConcurrentAccounts { get; set; }
    public long? TargetAccountId { get; set; }
}
