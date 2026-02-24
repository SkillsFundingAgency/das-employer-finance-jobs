using SFA.DAS.Provider.Events.Api.Types;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models.PaymentTransactions
{
    public class CreatePaymentTransactionLinesInput
    {
        public long AccountId { get; set; }
        public string PeriodEnd { get; set; }
        public string CorrelationId { get; set; }
        public IReadOnlyCollection<Payment> PaymentDetails { get; set; } // From RefreshPaymentDataActivity    
        public string IdempotencyKey { get; set; }  //Format: "account-{accountId}-period-{periodEnd}-payment-transactions"}

    }
}