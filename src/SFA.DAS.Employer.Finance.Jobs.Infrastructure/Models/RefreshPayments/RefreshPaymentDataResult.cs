using SFA.DAS.Provider.Events.Api.Types;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models.RefreshPayments
{
    public class RefreshPaymentDataResult
    {
        public int PaymentsCreated { get; set; }
        public IReadOnlyCollection<Payment> PaymentDetails { get; set; }//All payments (new + existing) for downstream activities
    }
}
