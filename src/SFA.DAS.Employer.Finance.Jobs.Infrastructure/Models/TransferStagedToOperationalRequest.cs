namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public class TransferStagedToOperationalRequest
{
    public long AccountId { get; set; }
    public string PeriodEnd { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
}
