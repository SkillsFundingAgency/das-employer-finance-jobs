namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public class ImportLevyRunSummary
{
    public int AccountsProcessed { get; set; }
    public int PayeDiscovered { get; set; }
    public int PayeProcessed { get; set; }
    public int LevyDeclarationsFetched { get; set; }
    public int LevyDeclarationsNormalized { get; set; }
    public int LevyDeclarationsPersisted { get; set; }
    public int LevyDeclarationsSkipped { get; set; }
    public int TransactionsCreated { get; set; }
    public int EnglishFractionsStored { get; set; }
    public int EnglishFractionsIgnored { get; set; }
    public int EnglishFractionsSkipped { get; set; }
    public int EnglishFractionCalculationDatesPersisted { get; set; }
    public int EnglishFractionCalculationDatesSkipped { get; set; }
    public int GetPayeSchemesRetries { get; set; }
    public int GetLastSubmissionDateRetries { get; set; }
    public int GetLastEnglishFractionDateRetries { get; set; }
    public int ImportLevyDeclarationsRetries { get; set; }
    public int GetEnglishFractionsRetries { get; set; }
    public int PersistEnglishFractionsRetries { get; set; }
    public int PersistEnglishFractionDateRetries { get; set; }
    public int GetExistingSubmissionIdsRetries { get; set; }
    public int GetExistingPeriod12DeclarationsRetries { get; set; }
    public int NormalizeLevyDeclarationsRetries { get; set; }
    public int PersistLevyDeclarationsRetries { get; set; }
    public int TotalFailures { get; set; }
    public int TotalRetries { get; set; }
}