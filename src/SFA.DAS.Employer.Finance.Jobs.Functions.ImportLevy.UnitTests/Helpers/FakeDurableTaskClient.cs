using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.UnitTests.Helpers;

public class FakeDurableTaskClient : DurableTaskClient
{
    public FakeDurableTaskClient() : base("fake")
    {
    }

    public override Task<string> ScheduleNewOrchestrationInstanceAsync(
        TaskName orchestratorName,
        object? input = null,
        StartOrchestrationOptions? options = null,
        CancellationToken cancellation = default)
    {
        return Task.FromResult(options?.InstanceId ?? Guid.NewGuid().ToString());
    }

    public override Task RaiseEventAsync(
        string instanceId,
        string eventName,
        object? eventPayload = null,
        CancellationToken cancellation = default)
    {
        return Task.CompletedTask;
    }

    public override Task<OrchestrationMetadata> WaitForInstanceStartAsync(
        string instanceId,
        bool getInputsAndOutputs = false,
        CancellationToken cancellation = default)
    {
        return Task.FromResult(new OrchestrationMetadata(Guid.NewGuid().ToString(), instanceId));
    }

    public override Task<OrchestrationMetadata> WaitForInstanceCompletionAsync(
        string instanceId,
        bool getInputsAndOutputs = false,
        CancellationToken cancellation = default)
    {
        return Task.FromResult(new OrchestrationMetadata(Guid.NewGuid().ToString(), instanceId));
    }

    public override Task TerminateInstanceAsync(
        string instanceId,
        object? output = null,
        CancellationToken cancellation = default)
    {
        return Task.CompletedTask;
    }

    public override Task SuspendInstanceAsync(
        string instanceId,
        string? reason = null,
        CancellationToken cancellation = default)
    {
        return Task.CompletedTask;
    }

    public override Task ResumeInstanceAsync(
        string instanceId,
        string? reason = null,
        CancellationToken cancellation = default)
    {
        return Task.CompletedTask;
    }

    public override Task<OrchestrationMetadata?> GetInstanceAsync(
        string instanceId,
        bool getInputsAndOutputs = false,
        CancellationToken cancellation = default)
    {
        return Task.FromResult<OrchestrationMetadata?>(new OrchestrationMetadata(Guid.NewGuid().ToString(), instanceId));
    }

    public override Task<OrchestrationMetadata?> GetInstancesAsync(
        string instanceId,
        bool getInputsAndOutputs = false,
        CancellationToken cancellation = default)
    {
        return Task.FromResult<OrchestrationMetadata?>(new OrchestrationMetadata(Guid.NewGuid().ToString(), instanceId));
    }

    public override AsyncPageable<OrchestrationMetadata> GetAllInstancesAsync(OrchestrationQuery? filter = null)
    {
        return new FakeOrchestrationMetadataAsyncPageable();
    }

    public override Task<PurgeResult> PurgeInstanceAsync(string instanceId, CancellationToken cancellation = default)
    {
        return Task.FromResult(new PurgeResult(1));
    }

    public override Task<PurgeResult> PurgeAllInstancesAsync(PurgeInstancesFilter filter, CancellationToken cancellation = default)
    {
        return Task.FromResult(new PurgeResult(1));
    }

    public override ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
