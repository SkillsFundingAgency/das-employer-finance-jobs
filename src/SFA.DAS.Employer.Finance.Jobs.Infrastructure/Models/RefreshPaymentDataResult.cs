namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public class RefreshPaymentDataResult
{
    public string CorrelationId { get; set; } = string.Empty;
    public int PaymentsCreated { get; set; }
    public List<Payment> PaymentDetails { get; set; } = new();
}
