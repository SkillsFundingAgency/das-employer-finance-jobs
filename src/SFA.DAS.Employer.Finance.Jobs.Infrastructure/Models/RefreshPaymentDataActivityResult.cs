using SFA.DAS.Provider.Events.Api.Types;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models
{
    public class RefreshPaymentDataActivityResult
    {
        public int PaymentsCreated { get; set; }
        public IReadOnlyCollection<Payment> PaymentDetails { get; set; } // Newly staged payments for downstream activities
        public string Status { get; set; }
        public string Message { get; set; }
    }
}
