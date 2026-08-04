namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public class TransferStagedToOperationalInput
{
    public long AccountId { get; set; }
    public string PeriodEndRef { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
}
