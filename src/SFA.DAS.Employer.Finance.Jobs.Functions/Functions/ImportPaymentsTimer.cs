using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Orchestrators;

namespace SFA.DAS.Employer.Finance.Jobs.Functions;

public class ImportPaymentsTimer(
    ILogger<ImportPaymentsTimer> logger,
    IOptions<ImportPaymentsOptions> importPaymentsOptions)
{
    private readonly ImportPaymentsOptions _options = importPaymentsOptions.Value;

    [Function("ImportPaymentsTimer")]
    public async Task Run([TimerTrigger("0 0 * * * *", RunOnStartup = true)] TimerInfo timerInfo, [DurableClient] DurableTaskClient client)
    {
        var correlationId = Guid.NewGuid().ToString();

        logger.LogInformation("[CorrelationId: {CorrelationId}] ImportPaymentsTimer triggered at {Time}", correlationId, DateTime.UtcNow);

        try
        {
            var instanceId = "ImportPaymentsOrchestrator-Singleton";
            var maxConcurrentAccounts = _options.GetMaxConcurrentAccounts();
            var maxConcurrentPeriodEnds = _options.GetMaxConcurrentPeriodEnds();

            var existingInstance = await client.GetInstanceAsync(instanceId);
            if (existingInstance != null && IsActive(existingInstance))
            {
                if (!IsStale(existingInstance, _options.GetActiveInstanceInactivityThreshold()))
                {
                    logger.LogWarning("[CorrelationId: {CorrelationId}] ImportPaymentsOrchestrator is already running. InstanceId: {InstanceId}", correlationId, existingInstance.InstanceId);
                    return;
                }

                if (!await TryStopStaleInstance(client, instanceId, existingInstance, correlationId))
                {
                    return;
                }
            }

            var newInstanceId = await client.ScheduleNewOrchestrationInstanceAsync(nameof(ImportPaymentsOrchestrator),
                new ImportPaymentsOrchestratorInput
                {
                    CorrelationId = correlationId,
                    TriggeredAt = DateTime.UtcNow,
                    MaxConcurrentAccounts = maxConcurrentAccounts,
                    MaxConcurrentPeriodEnds = maxConcurrentPeriodEnds,
                    TargetAccountId = _options.TargetAccountId
                },
                new StartOrchestrationOptions
                {
                    InstanceId = instanceId
                });

            logger.LogInformation("[CorrelationId: {CorrelationId}] Started ImportPaymentsOrchestrator with InstanceId: {InstanceId}", correlationId, newInstanceId);
            logger.LogInformation(
                "[CorrelationId: {CorrelationId}] ImportPayments configured with MaxConcurrentAccounts {MaxConcurrentAccounts} and MaxConcurrentPeriodEnds {MaxConcurrentPeriodEnds}",
                correlationId,
                maxConcurrentAccounts,
                maxConcurrentPeriodEnds);

            if (_options.TargetAccountId.HasValue)
            {
                logger.LogInformation(
                    "[CorrelationId: {CorrelationId}] ImportPayments is temporarily restricted to AccountId {TargetAccountId}",
                    correlationId,
                    _options.TargetAccountId.Value);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[CorrelationId: {CorrelationId}] Error starting ImportPaymentsOrchestrator: {ErrorMessage}", correlationId, ex.Message);
            throw;
        }
    }

    private static bool IsStale(OrchestrationMetadata existingInstance, TimeSpan inactivityThreshold)
    {
        var referenceTime = existingInstance.LastUpdatedAt == default
            ? existingInstance.CreatedAt
            : existingInstance.LastUpdatedAt;

        return DateTimeOffset.UtcNow - referenceTime > inactivityThreshold;
    }

    private static bool IsActive(OrchestrationMetadata instance) =>
        instance.RuntimeStatus == OrchestrationRuntimeStatus.Running
        || instance.RuntimeStatus == OrchestrationRuntimeStatus.Pending;

    private async Task<bool> TryStopStaleInstance(
        DurableTaskClient client,
        string instanceId,
        OrchestrationMetadata existingInstance,
        string correlationId)
    {
        logger.LogWarning(
            "[CorrelationId: {CorrelationId}] ImportPaymentsOrchestrator singleton is stale. InstanceId: {InstanceId}, CreatedAt: {CreatedAt}, LastUpdatedAt: {LastUpdatedAt}. Terminating and restarting.",
            correlationId,
            existingInstance.InstanceId,
            existingInstance.CreatedAt,
            existingInstance.LastUpdatedAt);

        try
        {
            await client.TerminateInstanceAsync(instanceId, "Terminated by ImportPaymentsTimer after inactivity threshold was exceeded.");
        }
        catch (OperationCanceledException ex)
        {
            logger.LogWarning(
                ex,
                "[CorrelationId: {CorrelationId}] Timed out terminating stale ImportPaymentsOrchestrator instance {InstanceId}. A new run will not be started on this timer tick.",
                correlationId,
                instanceId);

            return false;
        }

        var staleInstanceStopped = await WaitForStaleInstanceToStop(client, instanceId, correlationId);
        if (!staleInstanceStopped)
        {
            return false;
        }

        await TryPurgeStoppedStaleInstance(client, instanceId, correlationId);

        return true;
    }

    private async Task<bool> WaitForStaleInstanceToStop(DurableTaskClient client, string instanceId, string correlationId)
    {
        using var cancellationTokenSource = new CancellationTokenSource(_options.GetStaleInstanceTerminationTimeout());

        try
        {
            var completedInstance = await client.WaitForInstanceCompletionAsync(instanceId, cancellation: cancellationTokenSource.Token);

            if (completedInstance == null || !IsActive(completedInstance))
            {
                return true;
            }
        }
        catch (OperationCanceledException ex)
        {
            logger.LogWarning(
                ex,
                "[CorrelationId: {CorrelationId}] Timed out waiting for stale ImportPaymentsOrchestrator instance {InstanceId} to stop. Checking current status before starting a new run.",
                correlationId,
                instanceId);
        }

        var currentInstance = await client.GetInstanceAsync(instanceId);
        if (currentInstance == null || !IsActive(currentInstance))
        {
            return true;
        }

        logger.LogWarning(
            "[CorrelationId: {CorrelationId}] Stale ImportPaymentsOrchestrator instance {InstanceId} is still {RuntimeStatus}. A new run will not be started on this timer tick.",
            correlationId,
            instanceId,
            currentInstance.RuntimeStatus);

        return false;
    }

    private async Task TryPurgeStoppedStaleInstance(DurableTaskClient client, string instanceId, string correlationId)
    {
        using var cancellationTokenSource = new CancellationTokenSource(_options.GetStaleInstanceTerminationTimeout());

        try
        {
            await client.PurgeInstanceAsync(instanceId, cancellationTokenSource.Token);

            logger.LogInformation("[CorrelationId: {CorrelationId}] Terminated and purged stale ImportPaymentsOrchestrator instance {InstanceId}", correlationId, instanceId);
        }
        catch (OperationCanceledException ex)
        {
            logger.LogWarning(
                ex,
                "[CorrelationId: {CorrelationId}] Timed out purging stopped ImportPaymentsOrchestrator instance {InstanceId}. Starting a new run anyway because the stale instance is no longer active.",
                correlationId,
                instanceId);
        }
    }
}

public class ImportPaymentsOrchestratorInput
{
    public string CorrelationId { get; set; }
    public DateTime TriggeredAt { get; set; }
    public int MaxConcurrentAccounts { get; set; }
    public int MaxConcurrentPeriodEnds { get; set; }
    public long? TargetAccountId { get; set; }
}
