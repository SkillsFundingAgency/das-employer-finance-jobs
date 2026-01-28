using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Functions;
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
            var newPeriodEnds = await context.CallActivityAsync<List<PeriodEnd>>("GetNewPeriodEndsActivity",correlationId);

            result.NewPeriodEndsCount = newPeriodEnds?.Count ?? 0;
            result.TotalPeriodEndsCount = newPeriodEnds?.Count ?? 0;

            if (newPeriodEnds != null && newPeriodEnds.Count > 0)
            {
                logger.LogInformation("[CorrelationId: {CorrelationId}] Processing {Count} new period ends", correlationId, newPeriodEnds.Count);
              
                foreach (var periodEnd in newPeriodEnds)
                {
                    await context.CallActivityAsync("ProcessPeriodEndActivity",
                                                    new ProcessPeriodEndInput
                                                    {
                                                        CorrelationId = correlationId,
                                                        PeriodEnd = periodEnd
                                                    });
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

        logger.LogInformation("[CorrelationId: {CorrelationId}] Processing period end: Year={Year}, Period={Period}", input.CorrelationId, input.PeriodEnd.CalendarPeriodYear, input.PeriodEnd.PaymentsForPeriod);              
        await Task.CompletedTask;
    }
}

public class ProcessPeriodEndInput
{
    public string CorrelationId { get; set; }
    public PeriodEnd PeriodEnd { get; set; }
}