namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models.PaymentTransactions
{
    public class CreatePaymentTransactionLinesResult
    {
        public int TransactionsCreated { get; set; }
        public IReadOnlyCollection<PaymentTransactionLine> Transactions { get; set; }
    }

}
