namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public class AccountTransfer
{
    public long SenderAccountId { get; set; }
    public string? SenderAccountName { get; set; }
    public long ReceiverAccountId { get; set; }
    public string? ReceiverAccountName { get; set; }
    public string PeriodEnd { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
