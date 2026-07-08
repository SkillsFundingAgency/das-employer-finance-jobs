using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Functions;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.UnitTests.Helpers;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.UnitTests.Functions;

[TestFixture]
public class ImportLevyTimerTests
{
    private Mock<ILogger<ImportLevyTimer>> _logger = null!;

    [SetUp]
    public void SetUp()
    {
        _logger = new Mock<ILogger<ImportLevyTimer>>();
    }

    [Test]
    public async Task Run_Starts_Orchestrator_When_No_Existing_Instance()
    {
        var timer = new ImportLevyTimer(_logger.Object);
        var timerInfo = new TimerInfo();

        var clientMock = new Mock<FakeDurableTaskClient> { CallBase = true };
        clientMock
            .Setup(c => c.GetInstanceAsync(It.IsAny<string>(), It.IsAny<bool>(), default))
            .ReturnsAsync((OrchestrationMetadata?)null);

        await timer.Run(timerInfo, clientMock.Object);

        clientMock.Verify(c =>
            c.ScheduleNewOrchestrationInstanceAsync(
                "ImportLevyOrchestrator",
                It.Is<ImportLevyInput>(x => !string.IsNullOrWhiteSpace(x.CorrelationId)),
                It.Is<StartOrchestrationOptions>(o => o.InstanceId == "ImportLevyOrchestrator-Singleton"),
                default),
            Times.Once);

        _logger.VerifyLogContains("Started ImportLevyOrchestrator");
    }

    [TestCase(OrchestrationRuntimeStatus.Running)]
    [TestCase(OrchestrationRuntimeStatus.Pending)]
    public async Task Run_Does_Not_Start_Orchestrator_When_Instance_Is_Active(OrchestrationRuntimeStatus status)
    {
        var timer = new ImportLevyTimer(_logger.Object);
        var timerInfo = new TimerInfo();
        var metadata = OrchestrationMetadataHelper.Create("ImportLevyOrchestrator-Singleton", status);

        var clientMock = new Mock<FakeDurableTaskClient> { CallBase = true };
        clientMock
            .Setup(c => c.GetInstanceAsync(It.IsAny<string>(), It.IsAny<bool>(), default))
            .ReturnsAsync(metadata);

        await timer.Run(timerInfo, clientMock.Object);

        clientMock.Verify(c =>
            c.ScheduleNewOrchestrationInstanceAsync(
                It.IsAny<TaskName>(),
                It.IsAny<object>(),
                It.IsAny<StartOrchestrationOptions>(),
                default),
            Times.Never);

        _logger.VerifyLogContains("ImportLevyOrchestrator is already running");
    }

    [Test]
    public async Task Run_Logs_Error_And_Rethrows_When_Exception_Occurs()
    {
        var timer = new ImportLevyTimer(_logger.Object);
        var timerInfo = new TimerInfo();

        var clientMock = new Mock<FakeDurableTaskClient> { CallBase = true };
        clientMock
            .Setup(c => c.GetInstanceAsync(It.IsAny<string>(), It.IsAny<bool>(), default))
            .ThrowsAsync(new Exception("Boom"));

        Func<Task> act = async () => await timer.Run(timerInfo, clientMock.Object);

        var exceptionAssertion = await act.Should().ThrowAsync<InvalidOperationException>();
        exceptionAssertion.WithMessage("[CorrelationId: *] Failed to start ImportLevyOrchestrator.");
        exceptionAssertion.Which.InnerException.Should().BeOfType<Exception>().Which.Message.Should().Be("Boom");
        _logger.VerifyLogContains("Error starting ImportLevyOrchestrator");
    }
}
