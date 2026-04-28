namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Models;

public class NormalizeLevyDeclarationsResult
{
    public string CorrelationId { get; set; } = string.Empty;
    public long AccountId { get; set; }
    public string EmpRef { get; set; } = string.Empty;
    public List<NormalizedLevyDeclaration> Declarations { get; set; } = [];
    public int SourceDeclarationCount { get; set; }
    public int DuplicateDeclarationCount { get; set; }
    public int ExistingDeclarationCount { get; set; }
    public int FutureDeclarationCount { get; set; }
    public int PreLevyDeclarationCount { get; set; }
}
