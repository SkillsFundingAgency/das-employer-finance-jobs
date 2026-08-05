namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public class CreatePaymentTransactionLinesResult
{
    public int TransactionsCreated { get; set; }
    public IReadOnlyCollection<PaymentTransactionLine> Transactions { get; set; }
    public string Status { get; set; }
    public string Message { get; set; }
}
