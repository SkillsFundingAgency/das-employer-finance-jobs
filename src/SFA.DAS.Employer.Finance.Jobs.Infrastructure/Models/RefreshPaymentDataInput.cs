namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public class RefreshPaymentDataInput
{
    public long AccountId { get; set; }
    public PeriodEnd PeriodEnd { get; set; } = new();
    public string IdempotencyKey { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public int PaymentsCreated { get; set; }
    public List<Payment> PaymentDetails { get; set; } = new();
}
