using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;

public class EnglishFractionCalculationDatePersistenceService(
    IFinanceApiClient<FinanceApiConfiguration> financeApiClient,
    IEnglishFractionCalculationDateWriteTracker writeTracker,
    ILogger<EnglishFractionCalculationDatePersistenceService> logger) : IEnglishFractionCalculationDatePersistenceService
{
    public async Task<EnglishFractionCalculationDatePersistenceResult> PersistCalculationDateAsync(
        EnglishFractionsFetchResult input,
        CancellationToken cancellationToken = default)
    {
        var calculationDate = input.HmrcLatestUpdateDate.Date;

        if (!input.UpdateRequired)
        {
            logger.LogInformation(
                "[CorrelationId: {CorrelationId}] Skipping English fraction calculation date persistence because no update is required.",
                input.CorrelationId);

            return new EnglishFractionCalculationDatePersistenceResult
            {
                CorrelationId = input.CorrelationId,
                DateCalculated = calculationDate,
                UpdateRequired = false,
                Skipped = true
            };
        }

        if (calculationDate == DateTime.MinValue)
        {
            throw new ArgumentException("Calculation date must be provided when an update is required.", nameof(input));
        }

        if (!writeTracker.TryStartWrite(input.CorrelationId, calculationDate))
        {
            logger.LogInformation(
                "[CorrelationId: {CorrelationId}] English fraction calculation date {DateCalculated:yyyy-MM-dd} has already been persisted for this run.",
                input.CorrelationId,
                calculationDate);

            return new EnglishFractionCalculationDatePersistenceResult
            {
                CorrelationId = input.CorrelationId,
                DateCalculated = calculationDate,
                UpdateRequired = true,
                Skipped = true,
                AlreadyPersistedForRunDate = true
            };
        }

        var request = new PersistEnglishFractionCalculationDateRequest
        {
            Data = new PersistEnglishFractionCalculationDateRequestData
            {
                DateCalculated = calculationDate
            }
        };

        try
        {
            logger.LogInformation(
                "[CorrelationId: {CorrelationId}] Persisting English fraction calculation date {DateCalculated:yyyy-MM-dd}.",
                input.CorrelationId,
                calculationDate);

            await financeApiClient.Post<object>(request);
            writeTracker.MarkWriteSucceeded(input.CorrelationId, calculationDate);

            logger.LogInformation(
                "[CorrelationId: {CorrelationId}] Persisted English fraction calculation date {DateCalculated:yyyy-MM-dd}.",
                input.CorrelationId,
                calculationDate);

            return new EnglishFractionCalculationDatePersistenceResult
            {
                CorrelationId = input.CorrelationId,
                DateCalculated = calculationDate,
                UpdateRequired = true,
                Persisted = true
            };
        }
        catch
        {
            writeTracker.MarkWriteFailed(input.CorrelationId, calculationDate);
            throw;
        }
    }
}
