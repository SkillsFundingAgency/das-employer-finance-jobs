using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace SFA.DAS.Employer.Finance.Jobs;

public class EmployerFinacneSendJobFunction(ILogger<EmployerFinacneSendJobFunction> logger)
{ 

    [Function("EmployerFinacneSendJobTimerTrigger")]
    public async Task<List<FinanceJobsQueueItem>> Run([TimerTrigger("0 8 * * *")] TimerInfo myTimer)
    {
        logger.LogInformation($"Finance Job timer function executed at: {DateTime.UtcNow}");

        var returnList = new List<FinanceJobsQueueItem>();
        try
        {
            // Create 10 command messages and send them to the configured endpoint/queue.

            returnList = Enumerable.Range(0, 10)
                .Select(_ => new FinanceJobsQueueItem
                {
                    JobId = Guid.NewGuid(),
                    QueuedAt = DateTime.UtcNow,
                    Source = "EmployerFinacneSendJobFunction"
                }).ToList();


            logger.LogInformation("Enqueued {count} ProcessFinanceCommands. FirstJobId: {jobId}", returnList.Count, returnList.First().JobId);

            // Ensure the method actually awaits at least once
            await Task.CompletedTask;

            return returnList;

        }
        catch (Exception ex)
        {
            var firstJobId = returnList.FirstOrDefault()?.JobId ?? Guid.Empty;
            logger.LogError(ex, "Failed to enqueue {count} ProcessFinanceCommands. FirstJobId: {jobId}", returnList.Count, firstJobId);
            throw;
        }
    }
      
}
public class FinanceJobsQueueItem
{
    public Guid JobId { get; set; }
    public DateTime QueuedAt { get; set; }
    public string? Source { get; set; }
}