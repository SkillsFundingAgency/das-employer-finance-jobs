using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Activities;

public class PersistEnglishFractionCalculationDateActivity(
    IEnglishFractionCalculationDatePersistenceService persistenceService,
    ILogger<PersistEnglishFractionCalculationDateActivity> logger)
{
    [Function("PersistEnglishFractionCalculationDateActivity")]
    public async Task<EnglishFractionCalculationDatePersistenceResult> Run([ActivityTrigger] EnglishFractionsFetchResult input)
    {
        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] Processing English fraction calculation date persistence. UpdateRequired: {UpdateRequired}, DateCalculated: {DateCalculated:yyyy-MM-dd}",
            input.CorrelationId,
            input.UpdateRequired,
            input.HmrcLatestUpdateDate.Date);

        var result = await persistenceService.PersistCalculationDateAsync(input);

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] English fraction calculation date persistence completed. Persisted: {Persisted}, Skipped: {Skipped}, AlreadyPersistedForRunDate: {AlreadyPersistedForRunDate}",
            result.CorrelationId,
            result.Persisted,
            result.Skipped,
            result.AlreadyPersistedForRunDate);

        return result;
    }
}
