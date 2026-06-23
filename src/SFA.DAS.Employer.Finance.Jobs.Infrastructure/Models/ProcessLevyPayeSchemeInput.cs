namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public class ProcessLevyPayeSchemeInput
{
    public string CorrelationId { get; set; } = string.Empty;
    public long AccountId { get; set; }
    public string PayeSchemeReference { get; set; } = string.Empty;
}
