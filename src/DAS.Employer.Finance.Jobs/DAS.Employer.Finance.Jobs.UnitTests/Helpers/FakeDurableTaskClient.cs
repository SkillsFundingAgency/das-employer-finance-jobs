using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Helpers;

public class FakeDurableTaskClient : DurableTaskClient
{
    public FakeDurableTaskClient() : base("fake")
    {
    }

    public override Task<string> ScheduleNewOrchestrationInstanceAsync(TaskName orchestratorName, object input = null, StartOrchestrationOptions options = null,
        CancellationToken cancellation = new())
    {
        return Task.FromResult(options?.InstanceId ?? Guid.NewGuid().ToString());
    }

    public override Task RaiseEventAsync(string instanceId, string eventName, object eventPayload = null, CancellationToken cancellation = new())
    {
        return Task.CompletedTask;
    }

    public override Task<OrchestrationMetadata> WaitForInstanceStartAsync(string instanceId, bool getInputsAndOutputs = false,
        CancellationToken cancellation = new())
    {
        return Task.FromResult(new OrchestrationMetadata(Guid.NewGuid().ToString(), instanceId));
    }

    public override Task<OrchestrationMetadata> WaitForInstanceCompletionAsync(string instanceId, bool getInputsAndOutputs = false,
        CancellationToken cancellation = new())
    {
        return Task.FromResult(new OrchestrationMetadata(Guid.NewGuid().ToString(), instanceId));
    }

    public override Task TerminateInstanceAsync(string instanceId, object output = null, CancellationToken cancellation = new())
    {
        return Task.CompletedTask;
    }

    public override Task SuspendInstanceAsync(string instanceId, string reason = null, CancellationToken cancellation = new())
    {
        return Task.CompletedTask;
    }

    public override Task ResumeInstanceAsync(string instanceId, string reason = null, CancellationToken cancellation = new())
    {
        return Task.CompletedTask;
    }

    public override Task<OrchestrationMetadata> GetInstancesAsync(string instanceId, bool getInputsAndOutputs = false,
        CancellationToken cancellation = new())
    {
        return Task.FromResult(new OrchestrationMetadata(Guid.NewGuid().ToString(), instanceId));
    }

    public override AsyncPageable<OrchestrationMetadata> GetAllInstancesAsync(OrchestrationQuery filter = null)
    {
        return new FakeOrchestrationMetadataAsyncPageable();
    }

    public override Task<PurgeResult> PurgeInstanceAsync(string instanceId, CancellationToken cancellation = new())
    {
        return Task.FromResult(new PurgeResult(1));
    }

    public override Task<PurgeResult> PurgeAllInstancesAsync(PurgeInstancesFilter filter, CancellationToken cancellation = new())
    {
        return Task.FromResult(new PurgeResult(Random.Shared.Next()));
    }

    public override ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
