namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public class ImportLevyFailedItem
{
    public string CorrelationId { get; set; } = string.Empty;
    public long AccountId { get; set; }
    public string EmpRef { get; set; } = string.Empty;
    public string ActivityName { get; set; } = string.Empty;
    public string FailureReason { get; set; } = string.Empty;
    public int RetryAttempts { get; set; }
    public bool Retried => RetryAttempts > 0;
    public DateTime? FromDate { get; set; }
}