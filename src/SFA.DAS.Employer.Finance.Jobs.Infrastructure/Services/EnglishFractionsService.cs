using System.Globalization;
using HMRC.ESFA.Levy.Api.Types;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;

public class EnglishFractionsService(
    IHmrcClient hmrcClient,
    ILogger<EnglishFractionsService> logger) : IEnglishFractionsService
{
    public async Task<EnglishFractionsFetchResult> GetEnglishFractionsAsync(
        GetEnglishFractionsActivityInput input,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.EmployerReference))
        {
            throw new ArgumentException("Employer reference must be provided.", nameof(input));
        }

        var hmrcLatestUpdateDate = await hmrcClient.GetLastEnglishFractionUpdateAsync(cancellationToken);
        var hasExistingFractions = input.LastStoredFractionCalculatedDate.HasValue &&
                                   input.LastStoredFractionCalculatedDate.Value != DateTime.MinValue;

        var updateRequired = !hasExistingFractions || hmrcLatestUpdateDate > input.LastStoredFractionCalculatedDate!.Value;

        var result = new EnglishFractionsFetchResult
        {
            CorrelationId = input.CorrelationId,
            EmployerReference = input.EmployerReference,
            HmrcLatestUpdateDate = hmrcLatestUpdateDate,
            UpdateRequired = updateRequired
        };

        if (!updateRequired)
        {
            logger.LogInformation(
                "[CorrelationId: {CorrelationId}] No English fraction update required for PAYE {EmployerReference}. Latest local date {LastStoredDate}, HMRC date {HmrcDate}",
                input.CorrelationId,
                input.EmployerReference,
                input.LastStoredFractionCalculatedDate,
                hmrcLatestUpdateDate);

            return result;
        }

        DateTime? fromDate = null;

        if (hasExistingFractions)
        {
            fromDate = input.LastStoredFractionCalculatedDate!.Value.AddDays(-1);
        }

        result.RequestedFrom = fromDate;

        var fractionDeclarations = await hmrcClient.GetEnglishFractionsAsync(
            input.EmployerReference,
            fromDate,
            cancellationToken);

        result.Fractions = MapFractions(input.EmployerReference, fractionDeclarations, input.CorrelationId, logger);

        return result;
    }

    private static List<EnglishFraction> MapFractions(
        string employerReference,
        EnglishFractionDeclarations? fractionDeclarations,
        string correlationId,
        ILogger logger)
    {
        if (fractionDeclarations?.FractionCalculations == null)
        {
            return [];
        }

        var fractions = new List<EnglishFraction>();
        var sourceEmployerReference = string.IsNullOrWhiteSpace(fractionDeclarations.Empref)
            ? employerReference
            : fractionDeclarations.Empref;

        foreach (var calculation in fractionDeclarations.FractionCalculations)
        {
            if (calculation?.Fractions == null)
            {
                continue;
            }

            foreach (var fraction in calculation.Fractions)
            {
                if (decimal.TryParse(fraction.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
                {
                    fractions.Add(new EnglishFraction
                    {
                        EmployerReference = sourceEmployerReference,
                        DateCalculated = calculation.CalculatedAt,
                        Amount = amount
                    });
                    continue;
                }

                logger.LogError(
                    "[CorrelationId: {CorrelationId}] Could not convert HMRC English fraction value {FractionValue} for PAYE {EmployerReference}",
                    correlationId,
                    fraction.Value,
                    employerReference);
            }
        }

        return fractions;
    }
}
