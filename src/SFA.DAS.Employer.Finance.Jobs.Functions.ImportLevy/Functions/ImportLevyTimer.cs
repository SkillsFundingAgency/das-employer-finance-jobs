using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Orchestrators;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Functions;

public class ImportLevyTimer(ILogger<ImportLevyTimer> logger)
{
    [Function("ImportLevyTimer")]
    public async Task Run(
        [TimerTrigger("0 0 * * * *")] TimerInfo timerInfo,
        [DurableClient] DurableTaskClient client)
    {
        var correlationId = Guid.NewGuid().ToString();

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] ImportLevyTimer triggered at {Time}",
            correlationId,
            DateTime.UtcNow);

        try
        {
            const string instanceId = "ImportLevyOrchestrator-Singleton";

            var existingInstance = await client.GetInstanceAsync(instanceId);
            if (existingInstance != null &&
                (existingInstance.RuntimeStatus == OrchestrationRuntimeStatus.Running ||
                 existingInstance.RuntimeStatus == OrchestrationRuntimeStatus.Pending))
            {
                logger.LogWarning(
                    "[CorrelationId: {CorrelationId}] ImportLevyOrchestrator is already running. InstanceId: {InstanceId}",
                    correlationId,
                    existingInstance.InstanceId);
                return;
            }

            var newInstanceId = await client.ScheduleNewOrchestrationInstanceAsync(
                nameof(ImportLevyOrchestrator),
                new ImportLevyInput
                {
                    CorrelationId = correlationId,
                    TriggeredAt = DateTime.UtcNow
                },
                new StartOrchestrationOptions
                {
                    InstanceId = instanceId
                });

            logger.LogInformation(
                "[CorrelationId: {CorrelationId}] Started ImportLevyOrchestrator with InstanceId: {InstanceId}",
                correlationId,
                newInstanceId);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "[CorrelationId: {CorrelationId}] Error starting ImportLevyOrchestrator: {ErrorMessage}",
                correlationId,
                ex.Message);
            throw new InvalidOperationException(
                $"[CorrelationId: {correlationId}] Failed to start ImportLevyOrchestrator.",
                ex);
        }
    }
}