using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.Orchestrators;

public class ProcessAccountOrchestrationStarter([DurableClient] DurableTaskClient durableTaskClient) : IProcessAccountOrchestrationStarter
{
    public Task<OrchestrationMetadata?> GetInstanceAsync(string instanceId)
    {
        return durableTaskClient.GetInstanceAsync(instanceId);
    }

    public Task StartAsync(string orchestrationName, string instanceId, ProcessAccountInput input, CancellationToken cancellationToken)
    {
        return durableTaskClient.ScheduleNewOrchestrationInstanceAsync(
            orchestrationName,
            input,
            new StartOrchestrationOptions { InstanceId = instanceId },
            cancellationToken);
    }
}
