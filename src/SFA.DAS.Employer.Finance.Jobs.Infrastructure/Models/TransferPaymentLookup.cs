namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public class TransferPaymentLookup
{
    public Guid PaymentId { get; set; }
    public DateTime EvidenceSubmittedOn { get; set; }
    public int CollectionPeriodMonth { get; set; }
    public int CollectionPeriodYear { get; set; }
    public long Ukprn { get; set; }
}
