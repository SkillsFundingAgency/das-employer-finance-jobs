namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Models;

public class PersistLevyDeclarationsActivityResult
{
    public string CorrelationId { get; set; } = string.Empty;
    public long AccountId { get; set; }
    public string EmpRef { get; set; } = string.Empty;
    public bool Success { get; set; }
    public int DeclarationsSubmitted { get; set; }
    public int DeclarationsPersisted { get; set; }
    public int DeclarationsSkipped { get; set; }
    public int TransactionsCreated { get; set; }
    public string Message { get; set; } = string.Empty;
}
