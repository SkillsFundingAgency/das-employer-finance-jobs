using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Models;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Services;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Activities;

public class NormalizeLevyDeclarationsActivity(
    ILevyDeclarationNormalizer normalizer,
    ILogger<NormalizeLevyDeclarationsActivity> logger)
{
    [Function(nameof(NormalizeLevyDeclarationsActivity))]
    public NormalizeLevyDeclarationsResult Run([ActivityTrigger] NormalizeLevyDeclarationsInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] Normalizing {DeclarationCount} HMRC levy declarations for AccountId {AccountId}, EmpRef {EmpRef}",
            input.CorrelationId,
            input.HmrcDeclarations?.Count ?? 0,
            input.AccountId,
            input.EmpRef);

        var result = normalizer.Normalize(input);

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] Normalized {NormalizedCount} levy declarations for AccountId {AccountId}, EmpRef {EmpRef}. Removed Duplicate={DuplicateCount}, Existing={ExistingCount}, PreLevy={PreLevyCount}, Future={FutureCount}",
            result.CorrelationId,
            result.Declarations.Count,
            result.AccountId,
            result.EmpRef,
            result.DuplicateDeclarationCount,
            result.ExistingDeclarationCount,
            result.PreLevyDeclarationCount,
            result.FutureDeclarationCount);

        return result;
    }
}
