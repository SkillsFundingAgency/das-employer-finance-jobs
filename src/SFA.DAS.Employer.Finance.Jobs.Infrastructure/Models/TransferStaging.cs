namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public class TransferStaging
{
    public long TransferId { get; set; }
    public long SenderAccountId { get; set; }
    public long ReceiverAccountId { get; set; }
    public string ReceiverAccountName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime TransferDate { get; set; }
    public string PeriodEnd { get; set; } = string.Empty;
    public int CollectionPeriodMonth { get; set; }
    public int CollectionPeriodYear { get; set; }
    public long Ukprn { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
}
