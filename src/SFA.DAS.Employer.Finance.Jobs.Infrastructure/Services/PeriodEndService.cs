using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;

public class PeriodEndService(IFinanceApiClient<FinanceApiConfiguration> financeApiClient, IProviderPaymentApiClient<ProviderEventsApiConfiguration> providerPaymentApiClient, ILogger<PeriodEndService> logger) : IPeriodEndService
{
    public async Task<List<PeriodEnd>> GetNewPeriodEndsAsync(string correlationId)
    {
        logger.LogInformation("[CorrelationId: {CorrelationId}] Starting to retrieve period ends from external APIs", correlationId);

        var paymentPeriodEndsTask = GetPaymentPeriodEndsAsync(correlationId);

        var financePeriodEndsTask = GetFinancePeriodEndsAsync(correlationId);

        await Task.WhenAll(paymentPeriodEndsTask, financePeriodEndsTask);

        var paymentPeriodEnds = await paymentPeriodEndsTask;
        var financePeriodEnds = await financePeriodEndsTask;

        logger.LogInformation("[CorrelationId: {CorrelationId}] Retrieved {ProviderCount} period ends from Provider Events API and {FinanceCount} from Finance API",
                                                 correlationId, paymentPeriodEnds.Count, financePeriodEnds.Count);


        var newPeriodEnds = FilterNewPeriodEnds(paymentPeriodEnds, financePeriodEnds, correlationId);

        logger.LogInformation("[CorrelationId: {CorrelationId}] Found {NewCount} new period ends to process", correlationId, newPeriodEnds.Count);

        return newPeriodEnds;
    }

    public async Task<PeriodEnd> CreatePeriodEndAsync(PeriodEnd periodEnd, string correlationId)
    {
        try
        {
            logger.LogInformation($"[CorrelationId: {correlationId}] Calling Finance API to create periodEnd: {periodEnd.CalendarPeriodYear}-{periodEnd.CalendarPeriodMonth}");
            var request = new CreatePeriodEndRequest { Data = periodEnd };

            var createdPeriodEnd = await financeApiClient.Post<PeriodEnd>(request);

            logger.LogInformation($"[CorrelationId: {correlationId}] Successfully created periodEnd: {createdPeriodEnd.CalendarPeriodYear}-{createdPeriodEnd.CalendarPeriodMonth}");

            return createdPeriodEnd;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"[CorrelationId: {correlationId}] Error creating periodEnd: {periodEnd.CalendarPeriodYear}-{periodEnd.CalendarPeriodMonth}, Error Message: {ex.Message}");
            throw;
        }
    }
    private async Task<List<PeriodEnd>> GetPaymentPeriodEndsAsync(string correlationId)
    {
        try
        {
            logger.LogInformation("[CorrelationId: {CorrelationId}] Calling Provider Events API to get period ends", correlationId);

            var request = new GetPaymentPeriodEndsRequest();

            var response = await providerPaymentApiClient.GetWithResponseCode<List<PaymentPeriodEnd>>(request);
            var paymentPeriodEnds = response.Body;
            logger.LogInformation("[CorrelationId: {CorrelationId}] Successfully retrieved {Count} period ends from Provider Events API", correlationId, paymentPeriodEnds?.Count ?? 0);

            var periodEnds = paymentPeriodEnds?.ConvertAll(MapPaymentPeriodEnd);

            return periodEnds ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[CorrelationId: {CorrelationId}] Error retrieving period ends from Provider Events API: {ErrorMessage}", correlationId, ex.Message);
            throw;
        }
    }

    private static PeriodEnd MapPaymentPeriodEnd(PaymentPeriodEnd pe)
    {
        return new PeriodEnd
        {
            PeriodEndId = pe.Id,
            CalendarPeriodMonth = pe.CalendarPeriod.Month,
            CalendarPeriodYear = pe.CalendarPeriod.Year,
            AccountDataValidAt = pe.ReferenceData.AccountDataValidAt,
            CommitmentDataValidAt = pe.ReferenceData.CommitmentDataValidAt,
            CompletionDateTime = pe.CompletionDateTime,
            PaymentsForPeriod = pe.Links.PaymentsForPeriod
        };
    }

    private async Task<List<PeriodEnd>> GetFinancePeriodEndsAsync(string correlationId)
    {
        try
        {
            logger.LogInformation("[CorrelationId: {CorrelationId}] Calling Finance API to get existing period ends", correlationId);

            var request = new GetFinancePeriodEndsRequest();
            var response = await financeApiClient.GetWithResponseCode<List<PeriodEnd>>(request);
            var financePeriodEnds = response.Body ?? [];
            logger.LogInformation("[CorrelationId: {CorrelationId}] Successfully retrieved {Count} period ends from Finance API", correlationId, financePeriodEnds.Count);

            return financePeriodEnds;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[CorrelationId: {CorrelationId}] Error retrieving period ends from Finance API: {ErrorMessage}", correlationId, ex.Message);
            throw;
        }
    }
    private List<PeriodEnd> FilterNewPeriodEnds(List<PeriodEnd> paymentPeriodEnds, List<PeriodEnd> financePeriodEnds, string correlationId)
    {
        var existingPeriodEndIds = new HashSet<string>(financePeriodEnds.Select(p => p.PeriodEndId ?? string.Empty), StringComparer.OrdinalIgnoreCase);

        var newPeriodEnds = paymentPeriodEnds.Where(p => !string.IsNullOrEmpty(p.PeriodEndId) && !existingPeriodEndIds.Contains(p.PeriodEndId)).ToList();

        logger.LogInformation("[CorrelationId: {CorrelationId}] Filtered {NewCount} new period ends out of {TotalCount} provider period ends", correlationId, newPeriodEnds.Count, paymentPeriodEnds.Count);

        return newPeriodEnds;
    }
}
