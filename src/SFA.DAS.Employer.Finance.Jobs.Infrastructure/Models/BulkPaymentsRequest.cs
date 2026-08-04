namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public class BulkPaymentsRequest
{
    public List<PaymentStaging> Payments { get; set; }
}