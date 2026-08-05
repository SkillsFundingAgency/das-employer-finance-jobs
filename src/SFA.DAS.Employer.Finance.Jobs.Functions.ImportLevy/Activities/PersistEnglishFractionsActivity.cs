using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Activities;

public class PersistEnglishFractionsActivity(
    IEnglishFractionsPersistenceService persistenceService,
    ILogger<PersistEnglishFractionsActivity> logger)
{
    [Function("PersistEnglishFractionsActivity")]
    public async Task<EnglishFractionsPersistenceResult> Run([ActivityTrigger] EnglishFractionsFetchResult input)
    {
        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] Processing English fractions persistence for PAYE {EmployerReference}. UpdateRequired: {UpdateRequired}.",
            input.CorrelationId,
            input.EmployerReference,
            input.UpdateRequired);

        var result = await persistenceService.PersistEnglishFractionsAsync(input);

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] English fractions persistence completed for PAYE {EmployerReference}. Skipped: {Skipped}. Stored: {Stored}, Ignored: {Ignored}.",
            result.CorrelationId,
            result.EmployerReference,
            result.Skipped,
            result.Stored,
            result.Ignored);

        return result;
    }
}
