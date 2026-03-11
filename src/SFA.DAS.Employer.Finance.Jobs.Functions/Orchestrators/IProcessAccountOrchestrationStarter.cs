using Microsoft.DurableTask.Client;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.Orchestrators;

public interface IProcessAccountOrchestrationStarter
{
    Task<OrchestrationMetadata?> GetInstanceAsync(string instanceId);
    Task StartAsync(string orchestrationName, string instanceId, ProcessAccountInput input, CancellationToken cancellationToken);
}
