using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Functions.FundingProjection.Models;
using SFA.DAS.Employer.Finance.Jobs.Functions.FundingProjection.Orchestrators;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.FundingProjection.Handlers;

public static class FundingProjectionOrchestrationHandler
{
    public static async Task<string> StartAsync(
        DurableTaskClient client,
        ImportLearnersCostEvent input,
        ILogger logger,
        string correlationId)
    {
        var instanceId = GetInstanceId(input.JobId);

        try
        {
            var existingInstance = await client.GetInstanceAsync(instanceId);
            if (existingInstance != null && IsActive(existingInstance))
            {
                logger.LogWarning(
                    "[CorrelationId: {CorrelationId}] Funding projection job already has an active orchestration. InstanceId: {InstanceId}, RuntimeStatus: {RuntimeStatus}",
                    correlationId,
                    existingInstance.InstanceId,
                    existingInstance.RuntimeStatus);
                return existingInstance.InstanceId;
            }

            var newInstanceId = await client.ScheduleNewOrchestrationInstanceAsync(
                nameof(FundingProjectionOrchestrator),
                input,
                new StartOrchestrationOptions { InstanceId = instanceId });

            logger.LogInformation(
                "[CorrelationId: {CorrelationId}] Started FundingProjectionOrchestrator with InstanceId: {InstanceId}",
                correlationId,
                newInstanceId);

            return newInstanceId;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "[CorrelationId: {CorrelationId}] Error starting FundingProjectionOrchestrator: {ErrorMessage}",
                correlationId,
                ex.Message);
            throw new InvalidOperationException(
                $"[CorrelationId: {correlationId}] Failed to start FundingProjectionOrchestrator.",
                ex);
        }
    }

    public static string GetInstanceId(Guid jobId) => $"FundingProjection-{jobId:N}";

    private static bool IsActive(OrchestrationMetadata instance) =>
        instance.RuntimeStatus == OrchestrationRuntimeStatus.Running
        || instance.RuntimeStatus == OrchestrationRuntimeStatus.Pending
        || instance.RuntimeStatus == OrchestrationRuntimeStatus.Suspended;
}
