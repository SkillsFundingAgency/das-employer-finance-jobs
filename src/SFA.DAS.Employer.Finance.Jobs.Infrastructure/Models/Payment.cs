namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public class Payment
{
    public string PaymentId { get; set; } = string.Empty;

    public long AccountId { get; set; }

    public long ProviderId { get; set; }

    public decimal Amount { get; set; }

    public int CollectionPeriodMonth { get; set; }

    public int CollectionPeriodYear { get; set; }

    public DateTime PaymentDate { get; set; }

    public string FundingSource { get; set; } = string.Empty;

    public string LearnerReferenceNumber { get; set; } = string.Empty;

    public string ApprenticeshipId { get; set; } = string.Empty;
}
