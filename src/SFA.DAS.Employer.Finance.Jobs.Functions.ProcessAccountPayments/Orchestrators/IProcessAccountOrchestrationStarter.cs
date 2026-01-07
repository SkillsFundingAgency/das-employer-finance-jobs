using Microsoft.DurableTask.Client;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ProcessAccountPayments.Orchestrators
{
    public interface IProcessAccountOrchestrationStarter
    {
        Task<OrchestrationMetadata?> GetInstanceAsyc(string instanceId);

        Task StartAsyc(string orchestrationName, string instanceId, ProcessAccountInput input, CancellationToken cancellationToken);

    }
}
