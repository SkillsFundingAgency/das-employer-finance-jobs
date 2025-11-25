using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.InnerAPI.Requests;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces.Services;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;

public class PeriodEndService(IFinanceApiClient financeApiClient, IProviderEventsApiClient providerEventsApiClient,ILogger<PeriodEndService> logger) : IPeriodEndService
{  

    public async Task<List<PeriodEnd>> GetNewPeriodEndsAsync(string correlationId)
    {
        logger.LogInformation("[CorrelationId: {CorrelationId}] Starting to retrieve period ends from external APIs", correlationId);

        // Get period ends from Payment API
        var paymentPeriodEndsTask = GetPaymentPeriodEndsAsync(correlationId);
        // Get existing period ends from Finance API
        var financePeriodEndsTask = GetFinancePeriodEndsAsync(correlationId);

        await Task.WhenAll(paymentPeriodEndsTask, financePeriodEndsTask);

        var paymentPeriodEnds = await paymentPeriodEndsTask;
        var financePeriodEnds = await financePeriodEndsTask;

        logger.LogInformation("[CorrelationId: {CorrelationId}] Retrieved {ProviderCount} period ends from Provider Events API and {FinanceCount} from Finance API",
                                                 correlationId, paymentPeriodEnds.Count, financePeriodEnds.Count);

        // Filter to new period ends only
        var newPeriodEnds = FilterNewPeriodEnds(paymentPeriodEnds, financePeriodEnds, correlationId);

        logger.LogInformation("[CorrelationId: {CorrelationId}] Found {NewCount} new period ends to process", correlationId, newPeriodEnds.Count);

        return newPeriodEnds;
    }

    private async Task<List<PeriodEnd>> GetPaymentPeriodEndsAsync(string correlationId)
    {
        try
        {
            logger.LogInformation("[CorrelationId: {CorrelationId}] Calling Provider Events API to get period ends", correlationId);

            var request = new GetPaymentPeriodEndsRequest();
           
            var paymentPeriodEnds = await providerEventsApiClient.Get<List<PaymentPeriodEnd>>(request);

            logger.LogInformation("[CorrelationId: {CorrelationId}] Successfully retrieved {Count} period ends from payment period end API", correlationId, paymentPeriodEnds?.Count ?? 0);

            var periodEnds = paymentPeriodEnds?.Select(pe => new PeriodEnd
            {
                Id = 0,
                PeriodEndId = pe.Id,
                CalendarPeriodMonth = pe.CalendarPeriod.Month,
                CalendarPeriodYear = pe.CalendarPeriod.Year,
                AccountDataValidAt = pe.ReferenceData.CommitmentDataValidAt,
                CommitmentDataValidAt = pe.ReferenceData.AccountDataValidAt,
                CompletionDateTime = pe.CompletionDateTime,
                PaymentsForPeriod  = pe.Links.PaymentsForPeriod
            }).ToList();

            return periodEnds ?? new List<PeriodEnd>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex,"[CorrelationId: {CorrelationId}] Error retrieving period ends from Provider Events API: {ErrorMessage}", correlationId, ex.Message);
            throw;
        }
    }

    private async Task<List<PeriodEnd>> GetFinancePeriodEndsAsync(string correlationId)
    {
        try
        {
            logger.LogInformation("[CorrelationId: {CorrelationId}] Calling Finance API to get existing period ends", correlationId);

            var request = new GetFinancePeriodEndsRequest();
            // API returns List<PeriodEnd> directly
            var financePeriodEnds = await financeApiClient.Get<List<PeriodEnd>>(request);

            logger.LogInformation("[CorrelationId: {CorrelationId}] Successfully retrieved {Count} period ends from Finance API", correlationId, financePeriodEnds?.Count ?? 0);

            return financePeriodEnds ?? new List<PeriodEnd>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex,"[CorrelationId: {CorrelationId}] Error retrieving period ends from Finance API: {ErrorMessage}", correlationId, ex.Message);
            throw;
        }
    }

    //period ends in the list from payments which don't exist in the list from finance
    private List<PeriodEnd> FilterNewPeriodEnds(List<PeriodEnd> paymentPeriodEnds, List<PeriodEnd> financePeriodEnds, string correlationId)
    {       
        var existingPeriodEndIds = new HashSet<string>(financePeriodEnds.Select(p => p.PeriodEndId ?? string.Empty), StringComparer.OrdinalIgnoreCase);

        var newPeriodEnds = paymentPeriodEnds.Where(p => !string.IsNullOrEmpty(p.PeriodEndId) && !existingPeriodEndIds.Contains(p.PeriodEndId)).ToList();

        logger.LogInformation("[CorrelationId: {CorrelationId}] Filtered {NewCount} new period ends out of {TotalCount} provider period ends", correlationId, newPeriodEnds.Count, paymentPeriodEnds.Count);

        return newPeriodEnds;
    }
}
