namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Models;

public class PersistLevyDeclarationRequestData
{
    public long AccountId { get; set; }
    public List<PersistEmployerLevyData> EmployerLevyData { get; set; } = [];
    public bool GenerateTransactions { get; set; } = true;
}

public class PersistEmployerLevyData
{
    public string EmpRef { get; set; } = string.Empty;
    public PersistLevyDeclarations Declarations { get; set; } = new();
}

public class PersistLevyDeclarations
{
    public List<NormalizedLevyDeclaration> Declarations { get; set; } = [];
}
