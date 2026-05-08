namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Models;

public class PersistLevyDeclarationsResponse
{
    public int DeclarationsPersisted { get; set; }
    public int DeclarationsSkipped { get; set; }
    public int TransactionsCreated { get; set; }
}
