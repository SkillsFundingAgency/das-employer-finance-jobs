namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models
{
    public class AccountExistingPaymentIdsImportResult
    {
        public List<string> PaymentIds { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
    }
}
