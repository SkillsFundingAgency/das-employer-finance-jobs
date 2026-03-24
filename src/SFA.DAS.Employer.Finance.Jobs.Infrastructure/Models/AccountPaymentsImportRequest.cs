namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public class AccountPaymentsImportRequest
{
    public long AccountId { get; set; }
    public string PeriodEndRef { get; set; }
    public Guid CorrelationId { get; set; }
    public Guid IdempotencyKey { get; set; }
}
