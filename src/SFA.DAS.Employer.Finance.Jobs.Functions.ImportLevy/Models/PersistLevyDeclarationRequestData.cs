namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Models;

public class PersistLevyDeclarationRequestData
{
    public long AccountId { get; set; }
    public string EmpRef { get; set; } = string.Empty;
    public List<NormalizedLevyDeclaration> Declarations { get; set; } = [];
}