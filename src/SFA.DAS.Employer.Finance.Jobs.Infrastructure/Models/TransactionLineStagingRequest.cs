namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
public class TransactionLineStagingRequest
{
    public List<PaymentTransactionLine> TransactionLines { get; set; } = new();
}