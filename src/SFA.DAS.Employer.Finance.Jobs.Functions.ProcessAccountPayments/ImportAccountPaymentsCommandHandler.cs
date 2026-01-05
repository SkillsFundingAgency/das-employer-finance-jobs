using Microsoft.Extensions.Logging;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;


namespace SFA.DAS.Employer.Finance.Jobs.Functions.ProcessAccountPayments;

public class ImportAccountPaymentsCommandHandler(ILogger<ImportAccountPaymentsCommandHandler> logger, DurableTaskClient durableClient) : IHandleMessages<ImportAccountPaymentsCommand>
{
    public async Task Handle(ImportAccountPaymentsCommand message, IMessageHandlerContext context)
    {
        logger.LogInformation("Received ImportAccountPaymentsCommandHandler for AccountId: {AccountId}, PeriodEnd: {PeriodEndRef}", message.AccountId, message.PeriodEndRef);
        var correlationId = Guid.NewGuid().ToString();

        try
        {
            var instanceId = "ProcessAccountOrchestrator-Singleton";

            var existingInstance = await durableClient.GetInstanceAsync(instanceId, cancellation: context.CancellationToken);
            
            if (existingInstance != null && existingInstance.RuntimeStatus is OrchestrationRuntimeStatus.Running or OrchestrationRuntimeStatus.Pending)
            {
                logger.LogWarning("[CorrelationId: {CorrelationId}] ProcessAccountOrchestrator is already running. InstanceId: {InstanceId}", correlationId, existingInstance.InstanceId);
                return;
            }

            var instance = await durableClient.ScheduleNewOrchestrationInstanceAsync("ProcessAccountOrchestrator", message, new StartOrchestrationOptions
            {
                InstanceId = instanceId,
                StartAt = DateTime.UtcNow
            }, context.CancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[CorrelationId: {CorrelationId}] Error starting ProcessAccountOrchestrator: {ErrorMessage}", correlationId, ex.Message);
            throw;
        }
    }
}