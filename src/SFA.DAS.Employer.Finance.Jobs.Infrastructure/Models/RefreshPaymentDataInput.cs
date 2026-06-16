using SFA.DAS.Provider.Events.Api.Types;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models
{
    public class RefreshPaymentDataInput
    {
        public List<Payment> Payments { get; set; }
        public List<string> PaymentIds { get; set; }
        public long AccountId { get; set; }
        public string CorrelationId { get; set; } = string.Empty;
        public string IdempotencyKey { get; set; } = string.Empty; //Format: "account-{accountId}-period-{periodEnd}-payment-data"
    }
}
