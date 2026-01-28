using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.Orchestrators;

public class ProcessAccountOrchestrator(ILogger<ProcessAccountOrchestrator> logger)
{
    [Function("ProcessAccountOrchestrator")]
    public async Task<AccountProcessingResult> RunOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var input = context.GetInput<ProcessAccountInput>();
        var correlationId = input?.CorrelationId ?? Guid.NewGuid().ToString();

        logger.LogInformation("[CorrelationId: {CorrelationId}] ProcessAccountOrchestrator started", correlationId);
        
        // no activity yet        
        await Task.CompletedTask;  
        
        var result = new AccountProcessingResult
        {
          AccountId = input?.AccountId ?? 0,
          Success = true,
          PaymentsProcessed = 0,
          TransfersProcessed = 0
        };
        return result;
    }
}