namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public class CreatePeriodEndActivityInput
{
    public PeriodEnd PeriodEnd { get; set; }
    public Guid CorrelationId { get; set; }
}
