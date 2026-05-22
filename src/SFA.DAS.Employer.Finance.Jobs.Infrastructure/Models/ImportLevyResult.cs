namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public class ImportLevyResult
{
    public bool Success { get; set; }
    public int TotalAccountsCount { get; set; }
    public List<long> AccountIds { get; set; } = [];
    public List<PayeScheme> PayeSchemes { get; set; } = [];
    public List<ImportLevyDeclarationsActivityResult> LevyDeclarationsActivityResults { get; set; } = [];
    public List<EnglishFractionsFetchResult> EnglishFractionsFetchResults { get; set; } = [];
    public List<EnglishFractionsPersistenceResult> EnglishFractionsPersistenceResults { get; set; } = [];
    public List<EnglishFractionCalculationDatePersistenceResult> EnglishFractionCalculationDatePersistenceResults { get; set; } = [];
    public List<ImportLevyFailedItem> FailedItems { get; set; } = [];
    public ImportLevyRunSummary RunSummary { get; set; } = new();
    public string ErrorMessage { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
}
