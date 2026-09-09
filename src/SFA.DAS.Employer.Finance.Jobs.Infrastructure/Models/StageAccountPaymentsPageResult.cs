namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public class StageAccountPaymentsPageResult
{
    public const int MaxTransferLookupsPerPage = 10000;

    public int PageNumber { get; set; }
    public int TotalPages { get; set; }
    public int ItemCount { get; set; }
    public int PaymentsCreated { get; set; }
    public int MetadataCreated { get; set; }
    public int TransactionsCreated { get; set; }
    public IReadOnlyCollection<TransferPaymentLookup> TransferLookups { get; set; } = [];
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
