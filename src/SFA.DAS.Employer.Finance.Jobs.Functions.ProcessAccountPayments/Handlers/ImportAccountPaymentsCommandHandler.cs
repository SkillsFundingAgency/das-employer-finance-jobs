using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using NServiceBus;
using SFA.DAS.Employer.Finance.Jobs.Functions.Orchestrators;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.ProcessAccountPaymentsFunction.Orchestrators;
using SFA.DAS.Employer.Finance.Messages.Commands;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ProcessAccountPayments.Handlers;

public class ImportAccountPaymentsCommandHandler(ILogger<ImportAccountPaymentsCommandHandler> logger, IProcessAccountOrchestrationStarter starter) : IHandleMessages<ImportAccountPaymentsCommand>
{
    public async Task Handle(ImportAccountPaymentsCommand message, IMessageHandlerContext context)
    {
        logger.LogInformation("=== ProcessAccountPayments Handler Started ===");
        logger.LogInformation("Received ImportAccountPaymentsCommand for AccountId: {AccountId}, PeriodEnd: {PeriodEndRef}", message.AccountId, message.PeriodEndRef);
        logger.LogDebug("Message details: {@Message}", message);
        var correlationId = Guid.NewGuid().ToString();

        try
        {
            var instanceId = "ProcessAccountOrchestrator-Singleton";

            var existingInstance = await starter.GetInstanceAsyc(instanceId);

            if (existingInstance != null && (existingInstance.RuntimeStatus == OrchestrationRuntimeStatus.Running || existingInstance.RuntimeStatus == OrchestrationRuntimeStatus.Pending))
            {
                logger.LogWarning("[CorrelationId: {CorrelationId}] ProcessAccountOrchestrator is already running. InstanceId: {InstanceId}", correlationId, existingInstance.InstanceId);
                logger.LogInformation("=== ProcessAccountPayments Handler Completed (Skipped - Already Running) ===");
                return;
            }

            await starter.StartAsyc(nameof(ProcessAccountOrchestrator), instanceId,
             new ProcessAccountInput
             {
                 AccountId = message.AccountId,
                 PeriodEndRef = message.PeriodEndRef,
                 CorrelationId = correlationId,
                 IdempotencyKey = instanceId,
                 TriggeredAt = DateTime.UtcNow
             },
             context.CancellationToken);
             
            logger.LogInformation("[CorrelationId: {CorrelationId}] Started ProcessAccountOrchestrator for AccountId: {AccountId}, PeriodEnd: {PeriodEndRef}", correlationId, message.AccountId, message.PeriodEndRef);
            logger.LogInformation("=== ProcessAccountPayments Handler Completed Successfully ===");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[CorrelationId: {CorrelationId}] Error starting ProcessAccountOrchestrator: {ErrorMessage}", correlationId, ex.Message);
            logger.LogError("=== ProcessAccountPayments Handler Failed ===");
            throw;
        }
    }
}