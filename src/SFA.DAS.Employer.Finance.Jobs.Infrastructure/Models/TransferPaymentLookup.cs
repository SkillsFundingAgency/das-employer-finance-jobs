namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public class TransferPaymentLookup
{
    public Guid PaymentId { get; set; }
    public DateTime EvidenceSubmittedOn { get; set; }
    public int CollectionPeriodMonth { get; set; }
    public int CollectionPeriodYear { get; set; }
    public long Ukprn { get; set; }
    public long? ApprenticeshipId { get; set; }
    public long? StandardCode { get; set; }
    public int? FrameworkCode { get; set; }
    public int? ProgrammeType { get; set; }
    public int? PathwayCode { get; set; }
    public string? CourseCode { get; set; }
}
