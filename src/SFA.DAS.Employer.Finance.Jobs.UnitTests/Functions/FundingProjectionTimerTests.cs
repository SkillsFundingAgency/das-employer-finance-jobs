#nullable enable
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Functions;
using SFA.DAS.Employer.Finance.Jobs.Functions.Functions.TimerTriggers;
using SFA.DAS.Employer.Finance.Jobs.Functions.Orchestrators;
using SFA.DAS.Employer.Finance.Jobs.UnitTests.Helpers;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Functions;

[TestFixture]
public class FundingProjectionTimerWhenTriggered
{
    private Mock<ILogger<FundingProjectionTimer>> _logger = null!;

    [SetUp]
    public void SetUp()
    {
        _logger = new Mock<ILogger<FundingProjectionTimer>>();
    }

    [Test]
    public async Task Run_WhenNoActiveInstanceExists_ShouldStartOrchestrator()
    {
        // Arrange
        var timer = new FundingProjectionTimer(_logger.Object);
        var client = new Mock<FakeDurableTaskClient> { CallBase = true };
        client
            .Setup(x => x.GetInstanceAsync("FundingProjectionOrchestrator-Singleton", false, default))
            .ReturnsAsync((OrchestrationMetadata?)null);
        client
            .Setup(x => x.ScheduleNewOrchestrationInstanceAsync(
                nameof(FundingProjectionOrchestrator),
                null,
                It.Is<StartOrchestrationOptions>(options => options.InstanceId == "FundingProjectionOrchestrator-Singleton"),
                default))
            .ReturnsAsync("FundingProjectionOrchestrator-Singleton");

        // Act
        await timer.Run(new TimerInfo(), client.Object);

        // Assert
        client.Verify(x => x.ScheduleNewOrchestrationInstanceAsync(
            nameof(FundingProjectionOrchestrator),
            null,
            It.Is<StartOrchestrationOptions>(options => options.InstanceId == "FundingProjectionOrchestrator-Singleton"),
            default), Times.Once);
    }

    [TestCase(OrchestrationRuntimeStatus.Running)]
    [TestCase(OrchestrationRuntimeStatus.Pending)]
    public async Task Run_WhenActiveInstanceExists_ShouldNotStartAnotherOrchestrator(OrchestrationRuntimeStatus status)
    {
        // Arrange
        var timer = new FundingProjectionTimer(_logger.Object);
        var client = new Mock<FakeDurableTaskClient> { CallBase = true };
        client
            .Setup(x => x.GetInstanceAsync("FundingProjectionOrchestrator-Singleton", false, default))
            .ReturnsAsync(OrchestrationMetadataHelper.Create("FundingProjectionOrchestrator-Singleton", status));

        // Act
        await timer.Run(new TimerInfo(), client.Object);

        // Assert
        client.Verify(x => x.ScheduleNewOrchestrationInstanceAsync(
            It.IsAny<TaskName>(),
            It.IsAny<object>(),
            It.IsAny<StartOrchestrationOptions>(),
            default), Times.Never);
    }
}