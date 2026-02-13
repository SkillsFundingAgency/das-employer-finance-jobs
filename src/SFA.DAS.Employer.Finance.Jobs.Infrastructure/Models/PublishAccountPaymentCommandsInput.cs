namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public class PublishAccountPaymentCommandsInput
{
    public string PeriodEndRef { get; set; }
    public PeriodEnd PeriodEnd { get; set; }
}
