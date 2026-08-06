namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public class RefreshAccountTransfersInput
{
    public long AccountId { get; set; }
    public string? AccountName { get; set; }
    public string PeriodEndRef { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime TriggeredAt { get; set; }
    public IReadOnlyCollection<TransferPaymentLookup> Payments { get; set; } = [];
}
