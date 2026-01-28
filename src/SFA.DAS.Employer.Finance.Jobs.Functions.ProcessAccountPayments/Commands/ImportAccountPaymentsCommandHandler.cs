using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Functions.ProcessAccountPayments.Orchestrators;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ProcessAccountPayments.Commands;

public class ImportAccountPaymentsCommandHandler(ILogger<ImportAccountPaymentsCommandHandler> logger, IProcessAccountOrchestrationStarter starter) : IHandleMessages<ImportAccountPaymentsCommand>
{
    public async Task Handle(ImportAccountPaymentsCommand message, IMessageHandlerContext context)
    {
        logger.LogInformation("Received ImportAccountPaymentsCommandHandler for AccountId: {AccountId}, PeriodEnd: {PeriodEndRef}", message.AccountId, message.PeriodEndRef);
        var correlationId = Guid.NewGuid().ToString();

        try
        {
            var instanceId = "ProcessAccountOrchestrator-Singleton";

            var existingInstance = await starter.GetInstanceAsyc(instanceId);

            if (existingInstance != null && existingInstance.RuntimeStatus is OrchestrationRuntimeStatus.Running or OrchestrationRuntimeStatus.Pending)
            {
                logger.LogWarning("[CorrelationId: {CorrelationId}] ProcessAccountOrchestrator is already running. InstanceId: {InstanceId}", correlationId, existingInstance.InstanceId);
                return;
            }
            await starter.StartAsyc("ProcessAccountOrchestrator", instanceId,
             new ProcessAccountInput
             {
                 AccountId = message.AccountId,
                 PeriodEndRef = message.PeriodEndRef,
                 CorrelationId = correlationId,
                 IdempotencyKey = instanceId,
                 TriggeredAt = DateTime.UtcNow
             },
             context.CancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[CorrelationId: {CorrelationId}] Error starting ProcessAccountOrchestrator: {ErrorMessage}", correlationId, ex.Message);
            throw;
        }
    }
}