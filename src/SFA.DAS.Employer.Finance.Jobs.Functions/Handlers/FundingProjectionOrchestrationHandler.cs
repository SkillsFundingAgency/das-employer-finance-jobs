using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Functions.Orchestrators;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.Handlers;

public static class FundingProjectionOrchestrationHandler
{
    public const string InstanceId = "FundingProjectionOrchestrator-Singleton";

    public static async Task<string?> StartAsync(
        DurableTaskClient client,
        ILogger logger,
        string correlationId)
    {
        try
        {
            var existingInstance = await client.GetInstanceAsync(InstanceId);
            if (existingInstance != null && IsActive(existingInstance))
            {
                logger.LogWarning(
                    "[CorrelationId: {CorrelationId}] FundingProjectionOrchestrator is already running. InstanceId: {InstanceId}",
                    correlationId,
                    existingInstance.InstanceId);
                return null;
            }

            var newInstanceId = await client.ScheduleNewOrchestrationInstanceAsync(
                nameof(FundingProjectionOrchestrator),
                new StartOrchestrationOptions { InstanceId = InstanceId });

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

    private static bool IsActive(OrchestrationMetadata instance) =>
        instance.RuntimeStatus is OrchestrationRuntimeStatus.Running or OrchestrationRuntimeStatus.Pending;
}
