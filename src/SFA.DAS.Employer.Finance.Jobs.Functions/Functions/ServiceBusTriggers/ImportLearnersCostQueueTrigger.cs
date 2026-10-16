using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Functions.Handlers;
using SFA.DAS.Employer.Finance.Jobs.Functions.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.Functions.ServiceBusTriggers;

public sealed class ImportLearnersCostQueueTrigger(ILogger<ImportLearnersCostQueueTrigger> logger)
{
    [Function(nameof(ImportLearnersCostQueueTrigger))]
    public async Task Run(
        [ServiceBusTrigger("import-learners-cost", Connection = "ServiceBusConnection")] ImportLearnersCostEvent message,
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
