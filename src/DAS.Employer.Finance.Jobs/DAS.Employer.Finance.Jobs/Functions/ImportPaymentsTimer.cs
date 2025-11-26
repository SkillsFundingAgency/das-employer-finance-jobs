using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;

namespace SFA.DAS.Employer.Finance.Jobs.Functions;
public class ImportPaymentsTimer(ILogger<ImportPaymentsTimer> logger)
{   
    [Function("ImportPaymentsTimer")]
    public async Task Run([TimerTrigger("0 0/2 * * * *")] TimerInfo timerInfo, [DurableClient] DurableTaskClient starter)
    {
        var correlationId = Guid.NewGuid().ToString();

        logger.LogInformation("[CorrelationId: {CorrelationId}] ImportPaymentsTimer triggered at {Time}",correlationId, DateTime.UtcNow);

        try
        {
           
            var instanceId = "ImportPaymentsOrchestrator-Singleton";
            
            var existingInstance = await starter.GetInstanceAsync(instanceId);
            if (existingInstance != null && (existingInstance.RuntimeStatus == OrchestrationRuntimeStatus.Running 
                                          || existingInstance.RuntimeStatus == OrchestrationRuntimeStatus.Pending))
            {
                logger.LogWarning("[CorrelationId: {CorrelationId}] ImportPaymentsOrchestrator is already running. InstanceId: {InstanceId}", correlationId, existingInstance.InstanceId);
                return;
            }

            var newInstanceId = await starter.ScheduleNewOrchestrationInstanceAsync("ImportPaymentsOrchestrator",
                new ImportPaymentsOrchestratorInput
                {
                    CorrelationId = correlationId,
                    TriggeredAt = DateTime.UtcNow
                },
                new StartOrchestrationOptions
                {
                    InstanceId = instanceId
                });

            logger.LogInformation("[CorrelationId: {CorrelationId}] Started ImportPaymentsOrchestrator with InstanceId: {InstanceId}", correlationId, newInstanceId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,"[CorrelationId: {CorrelationId}] Error starting ImportPaymentsOrchestrator: {ErrorMessage}", correlationId, ex.Message);
            throw;
        }
    }
}

public class ImportPaymentsOrchestratorInput
{
    public string CorrelationId { get; set; }
    public DateTime TriggeredAt { get; set; }
}