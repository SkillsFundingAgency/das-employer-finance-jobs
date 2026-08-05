using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;

public class EnglishFractionsPersistenceService(
    IFinanceApiClient<FinanceApiConfiguration> financeApiClient,
    ILogger<EnglishFractionsPersistenceService> logger) : IEnglishFractionsPersistenceService
{
    public async Task<EnglishFractionsPersistenceResult> PersistEnglishFractionsAsync(
        EnglishFractionsFetchResult input,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.EmployerReference))
        {
            throw new ArgumentException("Employer reference must be provided.", nameof(input));
        }

        var result = new EnglishFractionsPersistenceResult
        {
            CorrelationId = input.CorrelationId,
            EmployerReference = input.EmployerReference,
            UpdateRequired = input.UpdateRequired,
            Skipped = !input.UpdateRequired
        };

        if (!input.UpdateRequired)
        {
            logger.LogInformation(
                "[CorrelationId: {CorrelationId}] Skipping English fractions persistence for PAYE {EmployerReference} because no update is required.",
                input.CorrelationId,
                input.EmployerReference);

            return result;
        }

        var request = new PersistEnglishFractionsRequest
        {
            Data = new PersistEnglishFractionsRequestData
            {
                EmpRef = input.EmployerReference,
                UpdateRequired = input.UpdateRequired,
                DateCalculated = input.HmrcLatestUpdateDate,
                Fractions = input.Fractions.Select(fraction => new PersistEnglishFractionItem
                {
                    DateCalculated = fraction.DateCalculated,
                    Amount = fraction.Amount
                }).ToList()
            }
        };

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] Persisting {FractionCount} English fractions for PAYE {EmployerReference}.",
            input.CorrelationId,
            input.Fractions.Count,
            input.EmployerReference);

        var response = await financeApiClient.Post<PersistEnglishFractionsResponse>(request);

        result.Stored = response?.Stored ?? 0;
        result.Ignored = response?.Ignored ?? 0;
        result.Skipped = false;

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] Persisted English fractions for PAYE {EmployerReference}. Stored: {Stored}, Ignored: {Ignored}.",
            input.CorrelationId,
            input.EmployerReference,
            result.Stored,
            result.Ignored);

        return result;
    }
}
