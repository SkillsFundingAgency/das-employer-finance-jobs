namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public class CreateTransferTransactionLinesResult
{
    public int TransactionsCreated { get; set; }
    public IReadOnlyCollection<PaymentTransactionLine> Transactions { get; set; } = [];
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
