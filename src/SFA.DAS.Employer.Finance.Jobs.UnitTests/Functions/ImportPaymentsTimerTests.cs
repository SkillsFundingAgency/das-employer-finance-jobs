using System.Threading;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Functions;
using SFA.DAS.Employer.Finance.Jobs.UnitTests.Helpers;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Functions;
public class ImportPaymentsTimerTests
{
    private Mock<ILogger<ImportPaymentsTimer>> _loggerMock;

    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<ImportPaymentsTimer>>();
    }

    [Test]
    public async Task Run_Should_Start_Orchestrator_When_No_Existing_Instance()
    {
        // Arrange
        var timer = new ImportPaymentsTimer(_loggerMock.Object);
        var timerInfo = new TimerInfo();
        var instanceId = "ImportPaymentsOrchestrator-Singleton";

        var clientMock = new Mock<FakeDurableTaskClient>() { CallBase = true };
        clientMock
            .Setup(c => c.GetInstanceAsync(instanceId, false, default))
            .ReturnsAsync((OrchestrationMetadata)null);

        clientMock
            .Setup(c => c.ScheduleNewOrchestrationInstanceAsync(
                "ImportPaymentsOrchestrator",
                It.Is<ImportPaymentsOrchestratorInput>(i => !string.IsNullOrEmpty(i.CorrelationId)),
                It.Is<StartOrchestrationOptions>(o => o.InstanceId == instanceId),
                CancellationToken.None))
            .ReturnsAsync(instanceId);

        // Act
        await timer.Run(timerInfo, clientMock.Object);

        // Assert
        clientMock.VerifyAll();
        clientMock.VerifyNoOtherCalls();
        _loggerMock.VerifyLogContains("Started ImportPaymentsOrchestrator");
    }

    [TestCase(OrchestrationRuntimeStatus.Running)]
    [TestCase(OrchestrationRuntimeStatus.Pending)]
    public async Task Run_Should_Not_Start_Orchestrator_When_Existing_Instance_Is_Active(OrchestrationRuntimeStatus status)
    {
        // Arrange
        var timer = new ImportPaymentsTimer(_loggerMock.Object);
        var timerInfo = new TimerInfo();
        var instanceId = "ImportPaymentsOrchestrator-Singleton";
        var metadata = OrchestrationMetadataHelper.Create(instanceId, status);

        var clientMock = new Mock<FakeDurableTaskClient>() { CallBase = true };
        clientMock
            .Setup(c => c.GetInstanceAsync(instanceId, false, default))
            .ReturnsAsync(metadata);

        // Act
        await timer.Run(timerInfo, clientMock.Object);

        // Assert
        clientMock.VerifyAll();
        clientMock.VerifyNoOtherCalls();
        _loggerMock.VerifyLogContains("ImportPaymentsOrchestrator is already running");
    }

    [Test]
    public async Task Run_Should_Log_Error_And_Rethrow_When_Exception_Occurs()
    {
        // Arrange
        var timer = new ImportPaymentsTimer(_loggerMock.Object);
        var timerInfo = new TimerInfo();
        var instanceId = "ImportPaymentsOrchestrator-Singleton";

        var clientMock = new Mock<FakeDurableTaskClient>() { CallBase = true };
        clientMock
            .Setup(c => c.GetInstanceAsync(instanceId, false, default))
            .ThrowsAsync(new Exception("Boom"));

        // Act
        Func<Task> act = async () => await timer.Run(timerInfo, clientMock.Object);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("Boom");

        clientMock.VerifyAll();
        clientMock.VerifyNoOtherCalls();
        _loggerMock.VerifyLogContains("Error starting ImportPaymentsOrchestrator");
    }

}
public static class LoggerExtensions
{
    public static void VerifyLogContains<T>(this Mock<ILogger<T>> loggerMock, string contains)
    {
        loggerMock.Verify(x =>
                x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString().Contains(contains)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce,
                $"Expected log containing '{contains}' but none was found");
    }
}
