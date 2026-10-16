using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Functions.Handlers;
using SFA.DAS.Employer.Finance.Jobs.Functions.Models;
using SFA.DAS.Employer.Finance.Jobs.Functions.ServiceBus;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.Functions.ServiceBusTriggers;

public sealed class FundingProjectionQueueTrigger(ILogger<FundingProjectionQueueTrigger> logger)
{
    [Function(nameof(FundingProjectionQueueTrigger))]
    public async Task Run(
        [ServiceBusTrigger(QueueConstants.FundingProjectionQueue, Connection = QueueConstants.ServiceBusConnection)] ImportLearnersCostEvent message,
        [DurableClient] DurableTaskClient client)
    {
        if (message.JobId == Guid.Empty || message.AccountId <= 0)
        {
            throw new ArgumentException("JobId and AccountId are required.");
        }

        await FundingProjectionOrchestrationHandler.StartAsync(
            client,
            message,
            logger,
            Guid.NewGuid().ToString());
    }
}
