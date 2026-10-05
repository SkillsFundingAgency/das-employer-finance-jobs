namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public class StageAccountPaymentsPageInput
{
    public long AccountId { get; set; }
    public string? AccountName { get; set; }
    public string PeriodEndRef { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTime TriggeredAt { get; set; }
    public int PageNumber { get; set; } = 1;
}
