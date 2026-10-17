using System.Threading;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Functions.FundingProjection.Handlers;
using SFA.DAS.Employer.Finance.Jobs.Functions.FundingProjection.Models;
using SFA.DAS.Employer.Finance.Jobs.UnitTests.Helpers;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Handlers;

public class WhenStartingFundingProjectionOrchestration
{
    private readonly ImportLearnersCostEvent _input = new(Guid.NewGuid(), 12345);
    private string _instanceId;
    private Mock<FakeDurableTaskClient> _client;

    [SetUp]
    public void Setup()
    {
        _instanceId = FundingProjectionOrchestrationHandler.GetInstanceId(_input.JobId);
        _client = new Mock<FakeDurableTaskClient> { CallBase = true };
        _client
            .Setup(c => c.ScheduleNewOrchestrationInstanceAsync(
                It.IsAny<TaskName>(),
                It.IsAny<object>(),
                It.IsAny<StartOrchestrationOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_instanceId);
    }

    [Test]
    public async Task Then_The_Orchestration_Is_Started_When_No_Instance_Exists()
    {
        _client.Setup(c => c.GetInstanceAsync(_instanceId, false, default)).ReturnsAsync((OrchestrationMetadata)null);

        await FundingProjectionOrchestrationHandler.StartAsync(_client.Object, _input, Mock.Of<ILogger>(), "correlation");

        VerifyScheduled(Times.Once());
    }

    [TestCase(OrchestrationRuntimeStatus.Completed)]
    [TestCase(OrchestrationRuntimeStatus.Failed)]
    [TestCase(OrchestrationRuntimeStatus.Terminated)]
    public async Task Then_The_Orchestration_Is_Restarted_When_The_Previous_Run_Has_Finished(OrchestrationRuntimeStatus status)
    {
        _client.Setup(c => c.GetInstanceAsync(_instanceId, false, default))
            .ReturnsAsync(OrchestrationMetadataHelper.Create(_instanceId, status));

        await FundingProjectionOrchestrationHandler.StartAsync(_client.Object, _input, Mock.Of<ILogger>(), "correlation");

        VerifyScheduled(Times.Once());
    }

    [TestCase(OrchestrationRuntimeStatus.Running)]
    [TestCase(OrchestrationRuntimeStatus.Pending)]
    [TestCase(OrchestrationRuntimeStatus.Suspended)]
    public async Task Then_The_Orchestration_Is_Not_Started_When_An_Instance_Is_Active(OrchestrationRuntimeStatus status)
    {
        _client.Setup(c => c.GetInstanceAsync(_instanceId, false, default))
            .ReturnsAsync(OrchestrationMetadataHelper.Create(_instanceId, status));

        await FundingProjectionOrchestrationHandler.StartAsync(_client.Object, _input, Mock.Of<ILogger>(), "correlation");

        VerifyScheduled(Times.Never());
    }

    private void VerifyScheduled(Times times) =>
        _client.Verify(c => c.ScheduleNewOrchestrationInstanceAsync(
            It.Is<TaskName>(n => n.Name == "FundingProjectionOrchestrator"),
            _input,
            It.Is<StartOrchestrationOptions>(o => o.InstanceId == _instanceId),
            It.IsAny<CancellationToken>()), times);
}
