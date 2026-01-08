using Grpc.Core;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using System;
using System.Threading.Tasks;

namespace SFA.DAS.Employer.Finance.Jobs.Functions;

public class ImportPaymentsTimer(ILogger<ImportPaymentsTimer> logger)
{
    [Function("ImportPaymentsTimer")]
    public async Task Run(
        [TimerTrigger("0 1/2 * * * *")] TimerInfo timerInfo,
        [DurableClient] DurableTaskClient starter)
    {
        var correlationId = Guid.NewGuid().ToString();
        var instanceId = "ProcessPeriodEndOrchestrator-Singleton";

        var isLocal =
            Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT")
                ?.Equals("Development", StringComparison.OrdinalIgnoreCase) == true;

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] ImportPaymentsTimer triggered at {Time} (IsLocal: {IsLocal})",
            correlationId,
            DateTime.UtcNow,
            isLocal);

        try
        {
            // Singleton guard ONLY in non-local environments
            if (!isLocal)
            {
                var existingInstance = await starter.GetInstanceAsync(instanceId);

                if (existingInstance != null)
                {
                    if (existingInstance.RuntimeStatus is
                        OrchestrationRuntimeStatus.Running or
                        OrchestrationRuntimeStatus.Pending or
                        OrchestrationRuntimeStatus.Suspended)
                    {
                        logger.LogInformation(
                            "[CorrelationId: {CorrelationId}] Orchestrator already active. InstanceId: {InstanceId}, Status: {Status}",
                            correlationId,
                            instanceId,
                            existingInstance.RuntimeStatus);

                        return;
                    }

                    if (existingInstance.RuntimeStatus is
                        OrchestrationRuntimeStatus.Completed or
                        OrchestrationRuntimeStatus.Failed or
                        OrchestrationRuntimeStatus.Terminated)
                    {
                        await starter.PurgeInstanceAsync(instanceId);

                        logger.LogInformation(
                            "[CorrelationId: {CorrelationId}] Purged existing instance. InstanceId: {InstanceId}, PreviousStatus: {Status}",
                            correlationId,
                            instanceId,
                            existingInstance.RuntimeStatus);
                    }
                    else
                    {
                        logger.LogWarning(
                            "[CorrelationId: {CorrelationId}] Instance exists in non-purgeable state. InstanceId: {InstanceId}, Status: {Status}. Not starting a new one.",
                            correlationId,
                            instanceId,
                            existingInstance.RuntimeStatus);

                        return;
                    }
                }
            }

            var now = DateTime.UtcNow;

            StartOrchestrationOptions options = null;

            // Enforce singleton only outside local dev
            if (!isLocal)
            {
                options = new StartOrchestrationOptions
                {
                    InstanceId = instanceId
                };
            }

            var newInstanceId =
                await starter.ScheduleNewOrchestrationInstanceAsync(
                    "ProcessPeriodEndOrchestrator",
                    new PeriodEnd
                    {
                        PeriodEndId = $"PER-{now:yyyyMM}",
                        CalendarPeriodMonth = now.Month,
                        CalendarPeriodYear = now.Year,
                        AccountDataValidAt = now,
                        CommitmentDataValidAt = now,
                        CompletionDateTime = now.AddDays(7),
                        PaymentsForPeriod = $"{now:yyyy-MM}"
                    },
                    options);

            logger.LogInformation(
                "[CorrelationId: {CorrelationId}] Started ProcessPeriodEndOrchestrator with InstanceId: {InstanceId}",
                correlationId,
                newInstanceId);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.AlreadyExists)
        {
            // Expected in race conditions (prod scale-out)
            logger.LogInformation(
                "[CorrelationId: {CorrelationId}] Orchestration instance already exists. InstanceId: {InstanceId}. Ignoring.",
                correlationId,
                instanceId);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "[CorrelationId: {CorrelationId}] Error starting ProcessPeriodEndOrchestrator: {ErrorMessage}",
                correlationId,
                ex.Message);
            throw;
        }
    }
}

public class ImportPaymentsOrchestratorInput
{
    public string CorrelationId { get; set; }
    public DateTime TriggeredAt { get; set; }
}
