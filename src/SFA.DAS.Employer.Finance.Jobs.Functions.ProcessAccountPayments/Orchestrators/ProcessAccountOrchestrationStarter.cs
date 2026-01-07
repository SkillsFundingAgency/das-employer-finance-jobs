using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ProcessAccountPayments.Orchestrators
{
    public class ProcessAccountOrchestrationStarter : IProcessAccountOrchestrationStarter
    {
        private readonly DurableTaskClient _durableTaskClient;

        public ProcessAccountOrchestrationStarter(DurableTaskClient durableTaskClient)
        {
            _durableTaskClient = durableTaskClient;
        }
        public Task<OrchestrationMetadata?> GetInstanceAsyc(string instanceId)
                                            => _durableTaskClient.GetInstanceAsync(instanceId);

        public Task StartAsyc(string orchestrationName, string instanceId, ProcessAccountInput input, CancellationToken cancellationToken)
                                                           => _durableTaskClient.ScheduleNewOrchestrationInstanceAsync(
                                                                orchestrationName,
                                                                input,
                                                                new StartOrchestrationOptions
                                                                {
                                                                    InstanceId = instanceId
                                                                },
                                                                cancellationToken);
        
    }
}
