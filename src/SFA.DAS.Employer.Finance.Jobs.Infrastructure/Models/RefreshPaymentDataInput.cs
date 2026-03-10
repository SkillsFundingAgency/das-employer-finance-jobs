namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models
{
    public class RefreshPaymentDataInput
    {
        public long AccountId { get; set; }
        public PeriodEnd PeriodEnd { get; set; } = null!;
        public string CorrelationId { get; set; } = string.Empty;
        public string IdempotencyKey { get; set; } = string.Empty;
    }
}
