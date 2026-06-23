using System.Collections.Generic;
using System.Linq;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Functions;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Orchestrators;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Orchestrators.ImportPaymentsOrchestator;

[TestFixture]
public class WhenImportPaymentsOrchestratorRun
{
    private Mock<ILogger<ImportPaymentsOrchestrator>> _loggerMock;
    private Mock<IPeriodEndService> _periodEndServiceMock;
    private Mock<TaskOrchestrationContext> _contextMock;
    private ImportPaymentsOrchestrator _orchestrator;

    [SetUp]
    public void SetUp()
    {
        _loggerMock = new Mock<ILogger<ImportPaymentsOrchestrator>>();
        _periodEndServiceMock = new Mock<IPeriodEndService>();
        _contextMock = new Mock<TaskOrchestrationContext>();
        _orchestrator = new ImportPaymentsOrchestrator(_loggerMock.Object, _periodEndServiceMock.Object);
    }

    [Test]
    public async Task Then_ShouldReturnSuccess_WhenPeriodEndsAreProcessed()
    {
        var correlationId = Guid.NewGuid().ToString();
        var input = new ImportPaymentsOrchestratorInput { CorrelationId = correlationId, TriggeredAt = DateTime.UtcNow, MaxConcurrentAccounts = 25 };
        var periodEnds = new List<PeriodEnd>
        {
            new PeriodEnd { PeriodEndId = "PE1", CalendarPeriodYear = 2024, PaymentsForPeriod = "Apr" },
            new PeriodEnd { PeriodEndId = "PE2", CalendarPeriodYear = 2024, PaymentsForPeriod = "May" }
        };

        _contextMock.Setup(c => c.GetInput<ImportPaymentsOrchestratorInput>()).Returns(input);
        _contextMock.Setup(c => c.CallActivityAsync<List<PeriodEnd>>(It.IsAny<TaskName>(), It.IsAny<string>(), It.IsAny<TaskOptions>()))
            .ReturnsAsync(periodEnds);
        _contextMock.Setup(c => c.CallActivityAsync<PeriodEnd>(
                It.Is<TaskName>(name => name.Name == nameof(ProcessPeriodEndOrchestrator.CreatePeriodEndActivity)),
                It.IsAny<CreatePeriodEndActivityInput>(),
                It.IsAny<TaskOptions>()))
            .ReturnsAsync((TaskName _, CreatePeriodEndActivityInput activityInput, TaskOptions _) => activityInput.PeriodEnd);
        _contextMock.Setup(c => c.CallSubOrchestratorAsync<PeriodEndResult>(
                It.IsAny<TaskName>(),
                It.IsAny<ProcessPeriodEndOrchestratorInput>(),
                It.IsAny<SubOrchestrationOptions>()))
            .ReturnsAsync(new PeriodEndResult());

        var result = await _orchestrator.RunOrchestrator(_contextMock.Object);

        result.Success.Should().BeTrue();
        result.CorrelationId.Should().Be(correlationId);
        result.NewPeriodEndsCount.Should().Be(periodEnds.Count);
        result.TotalPeriodEndsCount.Should().Be(periodEnds.Count);
        result.CreatedPeriodEndsCount.Should().Be(periodEnds.Count);
        result.FailedPeriodEndsCount.Should().Be(0);
        result.ErrorMessage.Should().BeNull();
    }

    [Test]
    public async Task Then_ShouldReturnSuccess_WhenNoPeriodEndsToProcess()
    {
        var correlationId = Guid.NewGuid().ToString();
        var input = new ImportPaymentsOrchestratorInput { CorrelationId = correlationId, TriggeredAt = DateTime.UtcNow, MaxConcurrentAccounts = 25 };
        var periodEnds = new List<PeriodEnd>();

        _contextMock.Setup(c => c.GetInput<ImportPaymentsOrchestratorInput>()).Returns(input);
        _contextMock.Setup(c => c.CallActivityAsync<List<PeriodEnd>>(It.IsAny<TaskName>(), It.IsAny<string>(), It.IsAny<TaskOptions>()))
            .ReturnsAsync(periodEnds);

        var result = await _orchestrator.RunOrchestrator(_contextMock.Object);

        result.Success.Should().BeTrue();
        result.CorrelationId.Should().Be(correlationId);
        result.NewPeriodEndsCount.Should().Be(0);
        result.TotalPeriodEndsCount.Should().Be(0);
        result.CreatedPeriodEndsCount.Should().Be(0);
        result.FailedPeriodEndsCount.Should().Be(0);
        result.ErrorMessage.Should().BeNull();
    }

    [Test]
    public async Task Then_ShouldReturnFailure_WhenExceptionIsThrown()
    {
        var correlationId = Guid.NewGuid().ToString();
        var input = new ImportPaymentsOrchestratorInput { CorrelationId = correlationId, TriggeredAt = DateTime.UtcNow, MaxConcurrentAccounts = 25 };

        _contextMock.Setup(c => c.GetInput<ImportPaymentsOrchestratorInput>()).Returns(input);
        _contextMock.Setup(c => c.CallActivityAsync<List<PeriodEnd>>(It.IsAny<TaskName>(), It.IsAny<string>(), It.IsAny<TaskOptions>()))
            .ThrowsAsync(new Exception("Test error"));

        var result = await _orchestrator.RunOrchestrator(_contextMock.Object);

        result.Success.Should().BeFalse();
        result.CorrelationId.Should().Be(correlationId);
        result.ErrorMessage.Should().Be("Test error");
    }

    [Test]
    public async Task Then_Should_Use_A_Sliding_Window_For_Period_End_Concurrency()
    {
        var correlationId = Guid.NewGuid().ToString();
        var input = new ImportPaymentsOrchestratorInput { CorrelationId = correlationId, TriggeredAt = DateTime.UtcNow, MaxConcurrentAccounts = 25 };
        var periodEnds = Enumerable.Range(1, 6)
            .Select(index => new PeriodEnd
            {
                PeriodEndId = $"PE{index}",
                CalendarPeriodYear = 2024,
                CalendarPeriodMonth = index,
                PaymentsForPeriod = $"P{index}"
            })
            .ToList();
        var firstBatch = Enumerable.Range(0, 5)
            .Select(_ => new TaskCompletionSource<PeriodEndResult>(TaskCreationOptions.RunContinuationsAsynchronously))
            .ToList();
        var scheduledCount = 0;

        _contextMock.Setup(c => c.GetInput<ImportPaymentsOrchestratorInput>()).Returns(input);
        _contextMock.Setup(c => c.CallActivityAsync<List<PeriodEnd>>(It.IsAny<TaskName>(), It.IsAny<string>(), It.IsAny<TaskOptions>()))
            .ReturnsAsync(periodEnds);
        _contextMock.Setup(c => c.CallActivityAsync<PeriodEnd>(
                It.Is<TaskName>(name => name.Name == nameof(ProcessPeriodEndOrchestrator.CreatePeriodEndActivity)),
                It.IsAny<CreatePeriodEndActivityInput>(),
                It.IsAny<TaskOptions>()))
            .ReturnsAsync((TaskName _, CreatePeriodEndActivityInput activityInput, TaskOptions _) => activityInput.PeriodEnd);
        _contextMock.Setup(c => c.CallSubOrchestratorAsync<PeriodEndResult>(
                It.IsAny<TaskName>(),
                It.IsAny<ProcessPeriodEndOrchestratorInput>(),
                It.IsAny<SubOrchestrationOptions>()))
            .Returns(() =>
            {
                scheduledCount++;
                if (scheduledCount <= firstBatch.Count)
                {
                    return firstBatch[scheduledCount - 1].Task;
                }

                return Task.FromResult(new PeriodEndResult());
            });

        var orchestrationTask = _orchestrator.RunOrchestrator(_contextMock.Object);

        await Task.Delay(100);

        scheduledCount.Should().Be(5, "the orchestrator should stop scheduling once the concurrency window is full");

        firstBatch[0].SetResult(new PeriodEndResult());

        await Task.Delay(100);

        scheduledCount.Should().Be(6, "completing one period end should allow the next one to be scheduled without waiting for the whole batch");

        foreach (var pendingTask in firstBatch.Skip(1))
        {
            pendingTask.SetResult(new PeriodEndResult());
        }

        var result = await orchestrationTask;

        result.Success.Should().BeTrue();
        scheduledCount.Should().Be(6);
    }

    [Test]
    public async Task Then_Should_Create_All_Period_Ends_Before_Scheduling_Account_Processing()
    {
        var correlationId = Guid.NewGuid().ToString();
        var input = new ImportPaymentsOrchestratorInput { CorrelationId = correlationId, TriggeredAt = DateTime.UtcNow, MaxConcurrentAccounts = 25 };
        var periodEnds = Enumerable.Range(1, 6)
            .Select(index => new PeriodEnd
            {
                PeriodEndId = $"PE{index}",
                CalendarPeriodYear = 2024,
                CalendarPeriodMonth = index,
                PaymentsForPeriod = $"P{index}"
            })
            .ToList();
        var createTasks = periodEnds
            .Select(_ => new TaskCompletionSource<PeriodEnd>(TaskCreationOptions.RunContinuationsAsynchronously))
            .ToList();
        var createdCount = 0;
        var accountProcessingScheduledCount = 0;

        _contextMock.Setup(c => c.GetInput<ImportPaymentsOrchestratorInput>()).Returns(input);
        _contextMock.Setup(c => c.CallActivityAsync<List<PeriodEnd>>(It.IsAny<TaskName>(), It.IsAny<string>(), It.IsAny<TaskOptions>()))
            .ReturnsAsync(periodEnds);
        _contextMock.Setup(c => c.CallActivityAsync<PeriodEnd>(
                It.Is<TaskName>(name => name.Name == nameof(ProcessPeriodEndOrchestrator.CreatePeriodEndActivity)),
                It.IsAny<CreatePeriodEndActivityInput>(),
                It.IsAny<TaskOptions>()))
            .Returns((TaskName _, CreatePeriodEndActivityInput activityInput, TaskOptions _) =>
            {
                var index = createdCount++;
                createTasks[index].SetResult(activityInput.PeriodEnd);
                return createTasks[index].Task;
            });
        _contextMock.Setup(c => c.CallSubOrchestratorAsync<PeriodEndResult>(
                It.IsAny<TaskName>(),
                It.IsAny<ProcessPeriodEndOrchestratorInput>(),
                It.IsAny<SubOrchestrationOptions>()))
            .Returns(() =>
            {
                accountProcessingScheduledCount++;
                return Task.FromResult(new PeriodEndResult());
            });

        var result = await _orchestrator.RunOrchestrator(_contextMock.Object);

        result.Success.Should().BeTrue();
        createdCount.Should().Be(periodEnds.Count);
        accountProcessingScheduledCount.Should().Be(periodEnds.Count);
        var invocations = _contextMock.Invocations.ToList();
        var firstAccountProcessingIndex = invocations.FindIndex(invocation =>
            invocation.Method.Name == nameof(TaskOrchestrationContext.CallSubOrchestratorAsync));
        var lastCreatePeriodEndIndex = invocations.FindLastIndex(invocation =>
            invocation.Method.Name == nameof(TaskOrchestrationContext.CallActivityAsync) &&
            invocation.Arguments.Count > 0 &&
            invocation.Arguments[0] is TaskName taskName &&
            taskName.Name == nameof(ProcessPeriodEndOrchestrator.CreatePeriodEndActivity));

        firstAccountProcessingIndex.Should().BeGreaterThan(lastCreatePeriodEndIndex);
        _contextMock.Invocations
            .Where(invocation => invocation.Method.Name == nameof(TaskOrchestrationContext.CallSubOrchestratorAsync))
            .Should()
            .OnlyContain(invocation => ((TaskName)invocation.Arguments[0]).Name == nameof(ProcessPeriodEndOrchestrator.ProcessPeriodEndAccountsOrchestrator));
    }

    [Test]
    public async Task Then_Should_Continue_When_One_Period_End_Create_Fails()
    {
        var correlationId = Guid.NewGuid().ToString();
        var input = new ImportPaymentsOrchestratorInput { CorrelationId = correlationId, TriggeredAt = DateTime.UtcNow, MaxConcurrentAccounts = 25 };
        var periodEnds = Enumerable.Range(1, 3)
            .Select(index => new PeriodEnd
            {
                PeriodEndId = $"PE{index}",
                CalendarPeriodYear = 2024,
                CalendarPeriodMonth = index,
                PaymentsForPeriod = $"P{index}"
            })
            .ToList();
        var accountProcessingScheduledCount = 0;

        _contextMock.Setup(c => c.GetInput<ImportPaymentsOrchestratorInput>()).Returns(input);
        _contextMock.Setup(c => c.CallActivityAsync<List<PeriodEnd>>(It.IsAny<TaskName>(), It.IsAny<string>(), It.IsAny<TaskOptions>()))
            .ReturnsAsync(periodEnds);
        _contextMock.Setup(c => c.CallActivityAsync<PeriodEnd>(
                It.Is<TaskName>(name => name.Name == nameof(ProcessPeriodEndOrchestrator.CreatePeriodEndActivity)),
                It.IsAny<CreatePeriodEndActivityInput>(),
                It.IsAny<TaskOptions>()))
            .Returns((TaskName _, CreatePeriodEndActivityInput activityInput, TaskOptions _) =>
            {
                if (activityInput.PeriodEnd.PeriodEndId == "PE2")
                {
                    return Task.FromException<PeriodEnd>(new InvalidOperationException("bad period end"));
                }

                return Task.FromResult(activityInput.PeriodEnd);
            });
        _contextMock.Setup(c => c.CallSubOrchestratorAsync<PeriodEndResult>(
                It.IsAny<TaskName>(),
                It.IsAny<ProcessPeriodEndOrchestratorInput>(),
                It.IsAny<SubOrchestrationOptions>()))
            .Returns(() =>
            {
                accountProcessingScheduledCount++;
                return Task.FromResult(new PeriodEndResult());
            });

        var result = await _orchestrator.RunOrchestrator(_contextMock.Object);

        result.Success.Should().BeTrue();
        result.CreatedPeriodEndsCount.Should().Be(2);
        result.FailedPeriodEndsCount.Should().Be(1);
        accountProcessingScheduledCount.Should().Be(2);
    }

    [Test]
    public async Task Then_Should_Continue_When_One_Period_End_Account_Processing_Fails()
    {
        var correlationId = Guid.NewGuid().ToString();
        var input = new ImportPaymentsOrchestratorInput { CorrelationId = correlationId, TriggeredAt = DateTime.UtcNow, MaxConcurrentAccounts = 25 };
        var periodEnds = Enumerable.Range(1, 3)
            .Select(index => new PeriodEnd
            {
                PeriodEndId = $"PE{index}",
                CalendarPeriodYear = 2024,
                CalendarPeriodMonth = index,
                PaymentsForPeriod = $"P{index}"
            })
            .ToList();
        var accountProcessingScheduledCount = 0;

        _contextMock.Setup(c => c.GetInput<ImportPaymentsOrchestratorInput>()).Returns(input);
        _contextMock.Setup(c => c.CallActivityAsync<List<PeriodEnd>>(It.IsAny<TaskName>(), It.IsAny<string>(), It.IsAny<TaskOptions>()))
            .ReturnsAsync(periodEnds);
        _contextMock.Setup(c => c.CallActivityAsync<PeriodEnd>(
                It.Is<TaskName>(name => name.Name == nameof(ProcessPeriodEndOrchestrator.CreatePeriodEndActivity)),
                It.IsAny<CreatePeriodEndActivityInput>(),
                It.IsAny<TaskOptions>()))
            .ReturnsAsync((TaskName _, CreatePeriodEndActivityInput activityInput, TaskOptions _) => activityInput.PeriodEnd);
        _contextMock.Setup(c => c.CallSubOrchestratorAsync<PeriodEndResult>(
                It.IsAny<TaskName>(),
                It.IsAny<ProcessPeriodEndOrchestratorInput>(),
                It.IsAny<SubOrchestrationOptions>()))
            .Returns((TaskName _, ProcessPeriodEndOrchestratorInput orchestratorInput, SubOrchestrationOptions _) =>
            {
                accountProcessingScheduledCount++;
                if (orchestratorInput.PeriodEnd.PeriodEndId == "PE2")
                {
                    return Task.FromException<PeriodEndResult>(new InvalidOperationException("bad account processing"));
                }

                return Task.FromResult(new PeriodEndResult { PeriodEndId = orchestratorInput.PeriodEnd.PeriodEndId });
            });

        var result = await _orchestrator.RunOrchestrator(_contextMock.Object);

        result.Success.Should().BeTrue();
        result.CreatedPeriodEndsCount.Should().Be(3);
        result.FailedPeriodEndsCount.Should().Be(0);
        accountProcessingScheduledCount.Should().Be(3);
    }
}
