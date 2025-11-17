using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.NServiceBus;

namespace SFA.DAS.Employer.Finance.Jobs;

public class EmployerFinanceSendJobFunction(IFunctionEndpoint functionEndpoint, ILogger<EmployerFinanceSendJobFunction> logger)
{
    [Function("EmployerFinanceSendJobTimerTrigger")]
    public async Task Run([TimerTrigger("0 8 * * *")] TimerInfo timerInfo,FunctionContext executionContext)
    {        
        logger.LogInformation("Process Finance Jobs Function executed at: {time}", DateTime.UtcNow);
        try
        {
            // Create 5 messages
            var messages = Enumerable.Range(1, 5).Select(i => new FinanceJobsQueueItem
            {
                JobId = Guid.NewGuid(),
                QueuedAt = DateTime.UtcNow,
                Source = "FinanceJobFunction"
            }).ToList();

            foreach (var msg in messages)
            {
                var options = new SendOptions();
                options.SetDestination(AzureFunctionQueueNames.ProcessFinanceJobQueue); // Service Bus queue

                
                await functionEndpoint.Send(msg, options, executionContext);
                logger.LogInformation("Sent ProcessFinanceCommand {JobId}", msg.JobId);
            }
        }
        catch (Exception ex)
        {           
            logger.LogError(ex, "Failed to process the finance jobs");
            throw;
        }
    }
}

public class FinanceJobsQueueItem : ICommand
{
    public Guid JobId { get; set; }
    public DateTime QueuedAt { get; set; }
    public string? Source { get; set; }
}