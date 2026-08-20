using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Orchestrators;

namespace SFA.DAS.Employer.Finance.Jobs.Functions;

public class ExpireFundsTimer(
    ILogger<ExpireFundsTimer> logger,
    IOptions<ExpireFundsOptions> expireFundsOptions)
{
    public const string ScheduleExpression = "0 0 0 28 * *";
    public const string SingletonInstanceId = "ExpireFundsOrchestrator-Singleton";

    private readonly ExpireFundsOptions _options = expireFundsOptions.Value;

    [Function(nameof(ExpireFundsTimer))]
    public async Task Run(
        [TimerTrigger(ScheduleExpression, RunOnStartup = false)] TimerInfo timerInfo,
        [DurableClient] DurableTaskClient client)
    {
        var triggeredAt = DateTime.UtcNow;
        var correlationId = Guid.NewGuid().ToString();

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] ExpireFundsTimer triggered at {TriggeredAt}",
            correlationId,
            triggeredAt);

        try
        {
            var existingInstance = await client.GetInstanceAsync(SingletonInstanceId);
            if (existingInstance != null && IsActive(existingInstance))
            {
                logger.LogWarning(
                    "[CorrelationId: {CorrelationId}] ExpireFundsOrchestrator is already running. InstanceId: {InstanceId}",
                    correlationId,
                    existingInstance.InstanceId);
                return;
            }

            var input = new ExpireFundsOrchestratorInput
            {
                CorrelationId = correlationId,
                TriggeredAt = triggeredAt,
                AccountPageSize = _options.GetAccountPageSize(),
                MaxConcurrentAccounts = _options.GetMaxConcurrentAccounts()
            };

            var newInstanceId = await client.ScheduleNewOrchestrationInstanceAsync(
                nameof(ExpireFundsOrchestrator),
                input,
                new StartOrchestrationOptions { InstanceId = SingletonInstanceId });

            logger.LogInformation(
                "[CorrelationId: {CorrelationId}] Started ExpireFundsOrchestrator with InstanceId {InstanceId}. AccountPageSize {AccountPageSize}, MaxConcurrentAccounts {MaxConcurrentAccounts}",
                correlationId,
                newInstanceId,
                input.AccountPageSize,
                input.MaxConcurrentAccounts);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "[CorrelationId: {CorrelationId}] Error starting ExpireFundsOrchestrator: {ErrorMessage}",
                correlationId,
                exception.Message);

            throw new InvalidOperationException(
                $"[CorrelationId: {correlationId}] Failed to start ExpireFundsOrchestrator.",
                exception);
        }
    }

    private static bool IsActive(OrchestrationMetadata instance) =>
        instance.RuntimeStatus is OrchestrationRuntimeStatus.Running
            or OrchestrationRuntimeStatus.Pending
            or OrchestrationRuntimeStatus.Suspended;
}
