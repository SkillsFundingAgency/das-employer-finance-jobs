namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public class GetAccountPayeSchemesActivityInput
{
    public string CorrelationId { get; set; } = string.Empty;
    public long AccountId { get; set; }
}
