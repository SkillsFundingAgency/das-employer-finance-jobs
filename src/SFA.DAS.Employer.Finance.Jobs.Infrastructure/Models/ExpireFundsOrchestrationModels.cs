namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public class ExpireFundsOrchestratorInput
{
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime TriggeredAt { get; set; }
    public int AccountPageSize { get; set; }
    public int MaxConcurrentAccounts { get; set; }
}

public class ProcessAccountExpireFundsInput
{
    public long AccountId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}

public class ProcessAccountExpireFundsResult
{
    public long AccountId { get; set; }
    public bool Success { get; set; }
    public bool FundsExpired { get; set; }
    public string? ErrorMessage { get; set; }
}

public class ExpireFundsOrchestrationResult
{
    public string CorrelationId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public int PagesProcessed { get; set; }
    public int TotalAccountsCount { get; set; }
    public int ProcessedAccountsCount { get; set; }
    public int SuccessfulAccountsCount { get; set; }
    public int FailedAccountsCount { get; set; }
    public int FundsExpiredAccountsCount { get; set; }
    public string? ErrorMessage { get; set; }
}
