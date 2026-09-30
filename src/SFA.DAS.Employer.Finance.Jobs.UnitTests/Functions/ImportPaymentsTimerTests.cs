using System.Threading;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SFA.DAS.Employer.Finance.Jobs.Functions;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Configuration;
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
        var timer = CreateTimer();
        var timerInfo = new TimerInfo();
        var instanceId = "ImportPaymentsOrchestrator-Singleton";

        var clientMock = new Mock<FakeDurableTaskClient>() { CallBase = true };
        clientMock
            .Setup(c => c.GetInstanceAsync(instanceId, false, default))
            .ReturnsAsync((OrchestrationMetadata)null);

        clientMock
            .Setup(c => c.ScheduleNewOrchestrationInstanceAsync(
                "ImportPaymentsOrchestrator",
                It.Is<ImportPaymentsOrchestratorInput>(i =>
                    !string.IsNullOrEmpty(i.CorrelationId)
                    && i.MaxConcurrentAccounts == ImportPaymentsOptions.DefaultMaxConcurrentAccounts
                    && i.MaxConcurrentPeriodEnds == ImportPaymentsOptions.DefaultMaxConcurrentPeriodEnds),
                It.Is<StartOrchestrationOptions>(o => o.InstanceId == instanceId),
                CancellationToken.None))
            .ReturnsAsync(instanceId);

        // Act
        await timer.Run(timerInfo, clientMock.Object);

        // Assert
        clientMock.VerifyAll();
        clientMock.VerifyNoOtherCalls();
        _loggerMock.VerifyLogContains("Started ImportPaymentsOrchestrator");
        _loggerMock.VerifyLogDoesNotContain("temporarily restricted");
    }

    [TestCase(OrchestrationRuntimeStatus.Running)]
    [TestCase(OrchestrationRuntimeStatus.Pending)]
    public async Task Run_Should_Not_Start_Orchestrator_When_Existing_Instance_Is_Active(OrchestrationRuntimeStatus status)
    {
        // Arrange
        var timer = CreateTimer();
        var timerInfo = new TimerInfo();
        var instanceId = "ImportPaymentsOrchestrator-Singleton";
        var metadata = OrchestrationMetadataHelper.Create(
            instanceId,
            status,
            createdAt: DateTimeOffset.UtcNow.AddMinutes(-30),
            lastUpdatedAt: DateTimeOffset.UtcNow.AddMinutes(-5));

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

    [TestCase(OrchestrationRuntimeStatus.Running)]
    [TestCase(OrchestrationRuntimeStatus.Pending)]
    public async Task Run_Should_Replace_Stale_Existing_Instance_Before_Starting_New_Run(OrchestrationRuntimeStatus status)
    {
        var timer = CreateTimer();
        var timerInfo = new TimerInfo();
        var instanceId = "ImportPaymentsOrchestrator-Singleton";
        var metadata = OrchestrationMetadataHelper.Create(
            instanceId,
            status,
            createdAt: DateTimeOffset.UtcNow.AddHours(-4),
            lastUpdatedAt: DateTimeOffset.UtcNow.AddHours(-2));

        var clientMock = new Mock<FakeDurableTaskClient>() { CallBase = true };
        clientMock
            .Setup(c => c.GetInstanceAsync(instanceId, false, default))
            .ReturnsAsync(metadata);
        clientMock
            .Setup(c => c.TerminateInstanceAsync(
                instanceId,
                It.Is<string>(message => message.Contains("inactivity threshold")),
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        clientMock
            .Setup(c => c.WaitForInstanceCompletionAsync(instanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(OrchestrationMetadataHelper.Create(instanceId, OrchestrationRuntimeStatus.Terminated));
        clientMock
            .Setup(c => c.PurgeInstanceAsync(instanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PurgeResult(1));
        clientMock
            .Setup(c => c.ScheduleNewOrchestrationInstanceAsync(
                "ImportPaymentsOrchestrator",
                It.Is<ImportPaymentsOrchestratorInput>(i => !string.IsNullOrEmpty(i.CorrelationId)),
                It.Is<StartOrchestrationOptions>(o => o.InstanceId == instanceId),
                CancellationToken.None))
            .ReturnsAsync(instanceId);

        await timer.Run(timerInfo, clientMock.Object);

        clientMock.VerifyAll();
        clientMock.Verify(c => c.GetInstanceAsync(instanceId, false, default), Times.Once);
        clientMock.Verify(c => c.TerminateInstanceAsync(
            instanceId,
            It.Is<string>(message => message.Contains("inactivity threshold")),
            CancellationToken.None), Times.Once);
        clientMock.Verify(c => c.PurgeInstanceAsync(instanceId, It.IsAny<CancellationToken>()), Times.Once);
        clientMock.Verify(c => c.ScheduleNewOrchestrationInstanceAsync(
            "ImportPaymentsOrchestrator",
            It.IsAny<ImportPaymentsOrchestratorInput>(),
            It.Is<StartOrchestrationOptions>(o => o.InstanceId == instanceId),
            CancellationToken.None), Times.Once);
        _loggerMock.VerifyLogContains("singleton is stale");
        _loggerMock.VerifyLogContains("Terminated and purged stale");
        _loggerMock.VerifyLogContains("Started ImportPaymentsOrchestrator");
    }

    [Test]
    public async Task Run_Should_Start_New_Run_When_Stale_Instance_Purge_Times_Out_After_Instance_Stopped()
    {
        var timer = CreateTimer();
        var timerInfo = new TimerInfo();
        var instanceId = "ImportPaymentsOrchestrator-Singleton";
        var metadata = OrchestrationMetadataHelper.Create(
            instanceId,
            OrchestrationRuntimeStatus.Running,
            createdAt: DateTimeOffset.UtcNow.AddHours(-4),
            lastUpdatedAt: DateTimeOffset.UtcNow.AddHours(-2));

        var clientMock = new Mock<FakeDurableTaskClient>() { CallBase = true };
        clientMock
            .Setup(c => c.GetInstanceAsync(instanceId, false, default))
            .ReturnsAsync(metadata);
        clientMock
            .Setup(c => c.TerminateInstanceAsync(
                instanceId,
                It.Is<string>(message => message.Contains("inactivity threshold")),
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        clientMock
            .Setup(c => c.WaitForInstanceCompletionAsync(instanceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(OrchestrationMetadataHelper.Create(instanceId, OrchestrationRuntimeStatus.Terminated));
        clientMock
            .Setup(c => c.PurgeInstanceAsync(instanceId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TaskCanceledException("purge timeout"));
        clientMock
            .Setup(c => c.ScheduleNewOrchestrationInstanceAsync(
                "ImportPaymentsOrchestrator",
                It.Is<ImportPaymentsOrchestratorInput>(i => !string.IsNullOrEmpty(i.CorrelationId)),
                It.Is<StartOrchestrationOptions>(o => o.InstanceId == instanceId),
                CancellationToken.None))
            .ReturnsAsync(instanceId);

        await timer.Run(timerInfo, clientMock.Object);

        clientMock.Verify(c => c.ScheduleNewOrchestrationInstanceAsync(
            "ImportPaymentsOrchestrator",
            It.IsAny<ImportPaymentsOrchestratorInput>(),
            It.Is<StartOrchestrationOptions>(o => o.InstanceId == instanceId),
            CancellationToken.None), Times.Once);
        _loggerMock.VerifyLogContains("Timed out purging stopped");
        _loggerMock.VerifyLogContains("Started ImportPaymentsOrchestrator");
    }

    [Test]
    public async Task Run_Should_Not_Start_New_Run_When_Stale_Instance_Is_Still_Active_After_Wait_Times_Out()
    {
        var timer = CreateTimer();
        var timerInfo = new TimerInfo();
        var instanceId = "ImportPaymentsOrchestrator-Singleton";
        var metadata = OrchestrationMetadataHelper.Create(
            instanceId,
            OrchestrationRuntimeStatus.Running,
            createdAt: DateTimeOffset.UtcNow.AddHours(-4),
            lastUpdatedAt: DateTimeOffset.UtcNow.AddHours(-2));

        var clientMock = new Mock<FakeDurableTaskClient>() { CallBase = true };
        clientMock
            .SetupSequence(c => c.GetInstanceAsync(instanceId, false, default))
            .ReturnsAsync(metadata)
            .ReturnsAsync(OrchestrationMetadataHelper.Create(
                instanceId,
                OrchestrationRuntimeStatus.Running,
                createdAt: DateTimeOffset.UtcNow.AddHours(-4),
                lastUpdatedAt: DateTimeOffset.UtcNow));
        clientMock
            .Setup(c => c.TerminateInstanceAsync(
                instanceId,
                It.Is<string>(message => message.Contains("inactivity threshold")),
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        clientMock
            .Setup(c => c.WaitForInstanceCompletionAsync(instanceId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TaskCanceledException("wait timeout"));

        await timer.Run(timerInfo, clientMock.Object);

        clientMock.Verify(c => c.ScheduleNewOrchestrationInstanceAsync(
            It.IsAny<TaskName>(),
            It.IsAny<object>(),
            It.IsAny<StartOrchestrationOptions>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _loggerMock.VerifyLogContains("Timed out waiting for stale");
        _loggerMock.VerifyLogContains("is still Running");
    }

    [Test]
    public async Task Run_Should_Log_Error_And_Rethrow_When_Exception_Occurs()
    {
        // Arrange
        var timer = CreateTimer();
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

    private ImportPaymentsTimer CreateTimer() =>
        CreateTimer(new ImportPaymentsOptions());

    private ImportPaymentsTimer CreateTimer(ImportPaymentsOptions options) =>
        new(_loggerMock.Object, Options.Create(options));

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

    public static void VerifyLogDoesNotContain<T>(this Mock<ILogger<T>> loggerMock, string contains)
    {
        loggerMock.Verify(x =>
                x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString().Contains(contains)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Never,
                $"Did not expect log containing '{contains}'");
    }
}
