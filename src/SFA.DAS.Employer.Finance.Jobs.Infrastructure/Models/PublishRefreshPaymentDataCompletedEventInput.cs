namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public class PublishRefreshPaymentDataCompletedEventInput
{
    public long AccountId { get; set; }
    public string PeriodEnd { get; set; } = string.Empty;
    public bool PaymentsProcessed { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}
