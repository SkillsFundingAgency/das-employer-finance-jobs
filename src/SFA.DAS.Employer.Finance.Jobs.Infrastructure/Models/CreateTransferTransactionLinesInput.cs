namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public class CreateTransferTransactionLinesInput
{
    public string PeriodEnd { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public IReadOnlyCollection<AccountTransfer> Transfers { get; set; } = [];
}
