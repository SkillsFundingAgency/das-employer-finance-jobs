using System.Reflection;
using System.Threading;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SFA.DAS.Employer.Finance.Jobs.Functions.ExpireFunds.Functions;
using SFA.DAS.Employer.Finance.Jobs.Functions.ExpireFunds.Orchestrators;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.UnitTests.Helpers;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Functions;

public class ExpireFundsTimerTests
{
    private Mock<ILogger<ExpireFundsTimer>> _loggerMock;

    [SetUp]
    public void SetUp()
    {
        _loggerMock = new Mock<ILogger<ExpireFundsTimer>>();
    }

    [Test]
    public void Then_The_Timer_Is_Scheduled_For_Midnight_On_The_28th_Of_Each_Month()
    {
        var timerParameter = typeof(ExpireFundsTimer)
            .GetMethod(nameof(ExpireFundsTimer.Run))!
            .GetParameters()[0];

        var timerTrigger = timerParameter.GetCustomAttribute<TimerTriggerAttribute>();

        timerTrigger.Should().NotBeNull();
        timerTrigger!.Schedule.Should().Be(ExpireFundsTimer.ScheduleExpression);
        timerTrigger.RunOnStartup.Should().BeFalse();
    }

    [Test]
    public async Task Then_A_Singleton_Orchestration_Is_Started_With_The_Configured_Throttling()
    {
        var options = new ExpireFundsOptions
        {
            AccountPageSize = 250,
            MaxConcurrentAccounts = 12
        };
        var clientMock = new Mock<FakeDurableTaskClient> { CallBase = true };

        clientMock
            .Setup(client => client.GetInstanceAsync(ExpireFundsTimer.SingletonInstanceId, false, default))
            .ReturnsAsync((OrchestrationMetadata)null);
        clientMock
            .Setup(client => client.ScheduleNewOrchestrationInstanceAsync(
                nameof(ExpireFundsOrchestrator),
                It.Is<ExpireFundsOrchestratorInput>(input =>
                    !string.IsNullOrWhiteSpace(input.CorrelationId)
                    && input.TriggeredAt != default
                    && input.AccountPageSize == 250
                    && input.MaxConcurrentAccounts == 12),
                It.Is<StartOrchestrationOptions>(orchestrationOptions =>
                    orchestrationOptions.InstanceId == ExpireFundsTimer.SingletonInstanceId),
                CancellationToken.None))
            .ReturnsAsync(ExpireFundsTimer.SingletonInstanceId);

        await CreateTimer(options).Run(new TimerInfo(), clientMock.Object);

        clientMock.VerifyAll();
        clientMock.VerifyNoOtherCalls();
        _loggerMock.VerifyLogContains(LogLevel.Information, "Started ExpireFundsOrchestrator");
    }

    [TestCase(OrchestrationRuntimeStatus.Running)]
    [TestCase(OrchestrationRuntimeStatus.Pending)]
    [TestCase(OrchestrationRuntimeStatus.Suspended)]
    public async Task Then_An_Active_Singleton_Prevents_Another_Run(OrchestrationRuntimeStatus runtimeStatus)
    {
        var metadata = OrchestrationMetadataHelper.Create(
            ExpireFundsTimer.SingletonInstanceId,
            runtimeStatus);
        var clientMock = new Mock<FakeDurableTaskClient> { CallBase = true };

        clientMock
            .Setup(client => client.GetInstanceAsync(ExpireFundsTimer.SingletonInstanceId, false, default))
            .ReturnsAsync(metadata);

        await CreateTimer(new ExpireFundsOptions()).Run(new TimerInfo(), clientMock.Object);

        clientMock.VerifyAll();
        clientMock.VerifyNoOtherCalls();
        _loggerMock.VerifyLogContains(LogLevel.Warning, "already running");
    }

    private ExpireFundsTimer CreateTimer(ExpireFundsOptions options) =>
        new(_loggerMock.Object, Options.Create(options));
}
