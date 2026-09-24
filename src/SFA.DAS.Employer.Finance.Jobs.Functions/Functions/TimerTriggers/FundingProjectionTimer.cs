using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Functions.Handlers;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.Functions.TimerTriggers;

public class FundingProjectionTimer(ILogger<FundingProjectionTimer> logger)
{
    // This function is triggered by a timer and starts the FundingProjectionOrchestrationHandler.
    [Function(nameof(FundingProjectionTimer))]
    public async Task Run(
        [TimerTrigger("0 0 5 * * *", RunOnStartup = false)] TimerInfo timerInfo,
        [DurableClient] DurableTaskClient client)
    {
        var correlationId = Guid.NewGuid().ToString();

        logger.LogInformation(
            "[CorrelationId: {CorrelationId}] FundingProjectionTimer triggered at {Time}",
            correlationId,
            DateTime.UtcNow);

        await FundingProjectionOrchestrationHandler.StartAsync(client, logger, correlationId);
    }
}