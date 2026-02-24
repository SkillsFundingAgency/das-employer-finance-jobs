using Microsoft.Extensions.Logging;
using NServiceBus;
using SFA.DAS.Employer.Finance.Messages.Commands;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ProcessAccountPayments.Handlers;

public class ImportAccountPaymentsCommandHandler(ILogger<ImportAccountPaymentsCommandHandler> logger) : IHandleMessages<ImportAccountPaymentsCommand>
{
    
    public async Task Handle(ImportAccountPaymentsCommand message, IMessageHandlerContext context)
    {
        logger.LogInformation("=== ProcessAccountPayments Handler Started ===");
        logger.LogInformation("Received ImportAccountPaymentsCommand for AccountId: {AccountId}, PeriodEnd: {PeriodEndRef}", message.AccountId, message.PeriodEndRef);
        logger.LogDebug("Message details: {@Message}", message);
        
        var correlationId = Guid.NewGuid().ToString();

        try
        {
            // TODO: Add DurableTask orchestration logic back when compatibility is resolved
            logger.LogInformation("[CorrelationId: {CorrelationId}] Processing ImportAccountPaymentsCommand for AccountId: {AccountId}, PeriodEnd: {PeriodEndRef}", 
                correlationId, message.AccountId, message.PeriodEndRef);
            
            // Simulate processing
            await Task.Delay(100, context.CancellationToken);
            
            logger.LogInformation("[CorrelationId: {CorrelationId}] Successfully processed ImportAccountPaymentsCommand for AccountId: {AccountId}, PeriodEnd: {PeriodEndRef}", 
                correlationId, message.AccountId, message.PeriodEndRef);
            logger.LogInformation("=== ProcessAccountPayments Handler Completed Successfully ===");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[CorrelationId: {CorrelationId}] Error processing ImportAccountPaymentsCommand: {ErrorMessage}", correlationId, ex.Message);
            logger.LogError("=== ProcessAccountPayments Handler Failed ===");
            throw;
        }
    }
}