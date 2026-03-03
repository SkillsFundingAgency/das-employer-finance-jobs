using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Messages.Commands;

namespace SFA.DAS.Employer.Finance.Jobs.ProcessAccountPaymentsFunction;

public class ProcessAccountPaymentsFunction
{
    private readonly ILogger<ProcessAccountPaymentsFunction> _logger;

    public ProcessAccountPaymentsFunction(ILogger<ProcessAccountPaymentsFunction> logger)
    {
        _logger = logger;
    }

    [Function(nameof(ProcessAccountPaymentsFunction))]
    public async Task Run(
        [ServiceBusTrigger("SFA.DAS.Employer.Finance.Jobs.ProcessAccountPayments", Connection = "AzureWebJobsServiceBus")]
        ImportAccountPaymentsCommand message,
        [DurableClient] DurableTaskClient starter)
    {
        _logger.LogInformation("Received a message in Function with  AccountID: {accountId}", message.AccountId);

        var correlationId = Guid.NewGuid().ToString();

        _logger.LogInformation("[CorrelationId: {CorrelationId}] ServiceBusTrigger triggered at {Time}", correlationId, DateTime.UtcNow);

        try
        {
            var instanceId = "ProcessAccountOrchestrator-Singleton";
            var existingInstance = await starter.GetInstanceAsync(instanceId);
            if (existingInstance != null && (existingInstance.RuntimeStatus == OrchestrationRuntimeStatus.Running
                                          || existingInstance.RuntimeStatus == OrchestrationRuntimeStatus.Pending))
            {
                _logger.LogWarning("[CorrelationId: {CorrelationId}] ProcessAccountOrchestrator is already running. InstanceId: {InstanceId}", correlationId, existingInstance.InstanceId);
                return;
            }

            var newInstanceId = await starter.ScheduleNewOrchestrationInstanceAsync("ProcessAccountOrchestrator",
                new ProcessAccountInput
                {
                    AccountId = message.AccountId,
                    PeriodEndRef = message.PeriodEndRef,
                    CorrelationId = correlationId,
                    IdempotencyKey = instanceId,
                    TriggeredAt = DateTime.UtcNow
                },
                new StartOrchestrationOptions
                {
                    InstanceId = instanceId
                });

            _logger.LogInformation("[CorrelationId: {CorrelationId}] Started ProcessAccountOrchestrator with InstanceId: {InstanceId}", correlationId, newInstanceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CorrelationId: {CorrelationId}] Error starting ProcessAccountOrchestrator: {ErrorMessage}", correlationId, ex.Message);
            throw;
        }
    }
}