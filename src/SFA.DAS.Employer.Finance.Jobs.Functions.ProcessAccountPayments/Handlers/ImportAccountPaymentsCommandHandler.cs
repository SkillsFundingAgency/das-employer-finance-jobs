using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Functions.Orchestrators;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.ProcessAccountPaymentsFunction.Orchestrators;
using SFA.DAS.Employer.Finance.Messages.Commands;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ProcessAccountPayments.Handlers;

public class ImportAccountPaymentsCommandHandler(ILogger<ImportAccountPaymentsCommandHandler> logger, IProcessAccountOrchestrationStarter starter) : IHandleMessages<ImportAccountPaymentsCommand>
{
    public async Task Handle(ImportAccountPaymentsCommand message, IMessageHandlerContext context)
    {
        logger.LogInformation("Received ImportAccountPaymentsCommand for AccountId: {AccountId}, PeriodEnd: {PeriodEndRef}", message.AccountId, message.PeriodEndRef);
        var correlationId = Guid.NewGuid().ToString();

        try
        {
            var instanceId = $"ProcessAccountOrchestrator-{message.AccountId}-{message.PeriodEndRef}";

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
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[CorrelationId: {CorrelationId}] Error starting ProcessAccountOrchestrator: {ErrorMessage}", correlationId, ex.Message);
            throw;
        }
    }
}