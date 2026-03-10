using Azure.Core;
using DurableTask.Core;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Functions;
using SFA.DAS.Employer.Finance.Jobs.Functions.Activities;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Orchestrators;

public class ImportPaymentsOrchestrator(ILogger<ImportPaymentsOrchestrator> logger, IPeriodEndService periodEndService)
{ 

    [Function("ImportPaymentsOrchestrator")]
    public async Task<ImportPaymentsResult> RunOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var input = context.GetInput<ImportPaymentsOrchestratorInput>();
        var correlationId = input?.CorrelationId ?? Guid.NewGuid().ToString();

        logger.LogInformation("[CorrelationId: {CorrelationId}] ImportPaymentsOrchestrator started", correlationId);

        var result = new ImportPaymentsResult
        {
            CorrelationId = correlationId,
            Success = false
        };

        try
        {
            var newPeriodEnds = await context.CallActivityAsync<List<PeriodEnd>>(nameof(GetNewPeriodEndsActivity), correlationId);

            result.NewPeriodEndsCount = newPeriodEnds?.Count ?? 0;
            result.TotalPeriodEndsCount = newPeriodEnds?.Count ?? 0;

            if (newPeriodEnds != null && newPeriodEnds.Count > 0)
            {
                logger.LogInformation("[CorrelationId: {CorrelationId}] Processing {Count} new period ends", correlationId, newPeriodEnds.Count);

                foreach (var periodEnd in newPeriodEnds)
                {
                    logger.LogInformation(
                        "[CorrelationId: {CorrelationId}] Retrieving levy accounts for period end {PeriodEndId}",
                        correlationId,
                        periodEnd.PeriodEndId);

                    var accounts = await context.CallActivityAsync<List<long>>(
                        nameof(GetLevyAccountsActivity),
                        correlationId);

                    if (accounts == null || accounts.Count == 0)
                    {
                        logger.LogInformation(
                            "[CorrelationId: {CorrelationId}] No levy accounts returned for processing",
                            correlationId);

                        continue;
                    }

                    logger.LogInformation(
                        "[CorrelationId: {CorrelationId}] Processing {AccountCount} accounts for period end {PeriodEndId}",
                        correlationId,
                        accounts.Count,
                        periodEnd.PeriodEndId);

                    var tasks = new List<Task>();

                    foreach (var accountId in accounts)
                    {
                        var idempotencyKey = $"account-{accountId}-period-{periodEnd.PeriodEndId}-payment-data";

                        await context.CallActivityAsync(
                         nameof(RefreshPaymentDataActivity),
                         new RefreshPaymentDataInput
                         {
                             AccountId = accountId,
                             PeriodEnd = periodEnd,
                             CorrelationId = correlationId,
                             IdempotencyKey = idempotencyKey
                         });
                    }

                    await Task.WhenAll(tasks);
                }
            }
            else
            {
                logger.LogInformation("[CorrelationId: {CorrelationId}] No new period ends to process", correlationId);
            }

            result.Success = true;

            logger.LogInformation("[CorrelationId: {CorrelationId}] ImportPaymentsOrchestrator completed successfully. Processed {Count} period ends", correlationId, result.NewPeriodEndsCount);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;

            logger.LogError(ex,"[CorrelationId: {CorrelationId}] ImportPaymentsOrchestrator failed: {ErrorMessage}", correlationId, ex.Message);
        }

        return result;
    }
      

    [Function("GetNewPeriodEndsActivity")]
    public async Task<List<PeriodEnd>> GetNewPeriodEndsActivity([ActivityTrigger] string correlationId)
    {
        logger.LogInformation("[CorrelationId: {CorrelationId}] GetNewPeriodEndsActivity started", correlationId);
        return await periodEndService.GetNewPeriodEndsAsync(correlationId);
    }

    //This activity will be here to process period end login TODO: Implement actual processing logic
    [Function("ProcessPeriodEndActivity")]
    public async Task ProcessPeriodEndActivity([ActivityTrigger] ProcessPeriodEndInput input)
    {
        // TODO: GET levy accounts and send ImportAccounPaymentsCommand per account via NServicebus.
        logger.LogInformation("[CorrelationId: {CorrelationId}] Processing period end: Year={Year}, Period={Period}", input.CorrelationId, input.PeriodEnd.CalendarPeriodYear, input.PeriodEnd.PaymentsForPeriod);              
        await Task.CompletedTask;
    }
}

public class ProcessPeriodEndInput
{
    public string CorrelationId { get; set; }
    public PeriodEnd PeriodEnd { get; set; }
}