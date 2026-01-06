ddingnamespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public class ProcessAccountInput
{
    public long AccountId { get; set; }
    public string PeriodEndRef { get; set; }
    public string CorrelationId { get; set; }
    public string IdempotencyKey { get; set; }
    public DateTime TriggeredAt { get; set; }
}