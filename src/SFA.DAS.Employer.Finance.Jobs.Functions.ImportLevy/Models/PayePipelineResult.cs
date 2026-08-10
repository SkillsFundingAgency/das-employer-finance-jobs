using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Models;

public class PayePipelineResult
{
    public ImportLevyDeclarationsActivityResult? LevyImportResult { get; init; }
    public EnglishFractionsFetchResult? EnglishFractionsFetchResult { get; init; }
    public EnglishFractionsPersistenceResult? EnglishFractionsPersistenceResult { get; init; }
    public EnglishFractionCalculationDatePersistenceResult? EnglishFractionCalculationDatePersistenceResult { get; init; }
    public PersistLevyDeclarationsActivityResult? PersistLevyDeclarationsActivityResult { get; init; }
    public ImportLevyFailedItem? FailedItem { get; init; }
}
