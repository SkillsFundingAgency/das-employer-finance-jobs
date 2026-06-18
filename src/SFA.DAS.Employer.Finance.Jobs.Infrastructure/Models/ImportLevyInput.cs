namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public class ImportLevyInput
{
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime TriggeredAt { get; set; }
}
