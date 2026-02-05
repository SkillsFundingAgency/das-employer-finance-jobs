using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.ProcessAccountPaymentsFunction.Orchestrators
{
    public class ProcessAccountOrchestrationStarter : IProcessAccountOrchestrationStarter
    {
        private readonly DurableTaskClient _durableTaskClient;
        
        public ProcessAccountOrchestrationStarter([DurableClient] DurableTaskClient durableTaskClient)
        {
            _durableTaskClient = durableTaskClient;
        }
        public async Task<OrchestrationMetadata?> GetInstanceAsyc(string instanceId)
        {
            return await _durableTaskClient.GetInstanceAsync(instanceId);
        }


        public async Task StartAsyc(string orchestrationName, string instanceId, ProcessAccountInput input, CancellationToken cancellationToken)
        {
            await _durableTaskClient.ScheduleNewOrchestrationInstanceAsync(
                                                                orchestrationName,
                                                                input,
                                                                new StartOrchestrationOptions
                                                                {
                                                                    InstanceId = instanceId
                                                                },
                                                                cancellationToken);
        }


    }
}