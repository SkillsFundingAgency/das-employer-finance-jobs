using System.Collections.Generic;
using System.Linq;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;
using SFA.DAS.Employer.Finance.Jobs.Orchestrators;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Orchestrators.ImportPaymentsOrchestator;

[TestFixture]
public class WhenProcessingPeriodEndOrchestratorRun
{
    private Mock<ILogger<ProcessPeriodEndOrchestrator>> _loggerMock;
    private Mock<IPeriodEndService> _periodEndServiceMock;
    private Mock<IAccountService> _accountServiceMock;
    private Mock<TaskOrchestrationContext> _contextMock;
    private ProcessPeriodEndOrchestrator _orchestrator;

    [SetUp]
    public void SetUp()
    {
        _loggerMock = new Mock<ILogger<ProcessPeriodEndOrchestrator>>();
        _periodEndServiceMock = new Mock<IPeriodEndService>();
        _accountServiceMock = new Mock<IAccountService>();
        _contextMock = new Mock<TaskOrchestrationContext>();
        _orchestrator = new ProcessPeriodEndOrchestrator(_loggerMock.Object, _periodEndServiceMock.Object, _accountServiceMock.Object);
    }

    [Test]
    public async Task Then_Should_Use_A_Sliding_Window_For_Account_Concurrency()
    {
        var correlationId = Guid.NewGuid().ToString();
        var inputPeriodEnd = CreatePeriodEnd("2425-R12");
        var createdPeriodEnd = CreatePeriodEnd("2425-R12", 101);
        var input = new ProcessPeriodEndOrchestratorInput
        {
            CorrelationId = correlationId,
            PeriodEnd = inputPeriodEnd,
            MaxConcurrentAccounts = 2
        };
        var accounts = new List<Accounts>
        {
            new() { Id = 1, Name = "A1" },
            new() { Id = 2, Name = "A2" },
            new() { Id = 3, Name = "A3" }
        };
        var accountTasks = Enumerable.Range(0, 3)
            .Select(_ => new TaskCompletionSource<AccountProcessingResult>(TaskCreationOptions.RunContinuationsAsynchronously))
            .ToList();
        var scheduledCount = 0;

        _contextMock.Setup(c => c.GetInput<ProcessPeriodEndOrchestratorInput>()).Returns(input);
        _contextMock.SetupGet(c => c.CurrentUtcDateTime).Returns(new DateTime(2026, 4, 22, 12, 0, 0, DateTimeKind.Utc));
        _contextMock.Setup(c => c.CallActivityAsync<PeriodEnd>(
                It.Is<TaskName>(name => name.Name == nameof(ProcessPeriodEndOrchestrator.CreatePeriodEndActivity)),
                It.IsAny<CreatePeriodEndActivityInput>(),
                It.IsAny<TaskOptions>()))
            .ReturnsAsync(createdPeriodEnd);
        _contextMock.Setup(c => c.CallActivityAsync<List<Accounts>>(
                It.Is<TaskName>(name => name.Name == nameof(ProcessPeriodEndOrchestrator.GetAccountsPageActivity)),
                It.IsAny<GetAccountsRequest>(),
                It.IsAny<TaskOptions>()))
            .ReturnsAsync(accounts);
        _contextMock.Setup(c => c.CallSubOrchestratorAsync<AccountProcessingResult>(
                It.Is<TaskName>(name => name.Name == nameof(ProcessAccountOrchestrator)),
                It.IsAny<ProcessAccountInput>(),
                It.IsAny<SubOrchestrationOptions>()))
            .Returns(() =>
            {
                scheduledCount++;
                return accountTasks[scheduledCount - 1].Task;
            });

        var orchestrationTask = _orchestrator.Run(_contextMock.Object);

        await Task.Delay(100);

        scheduledCount.Should().Be(2, "the account fan-out should stop scheduling once the account concurrency window is full");

        accountTasks[0].SetResult(new AccountProcessingResult { AccountId = 1, Success = true });

        await Task.Delay(100);

        scheduledCount.Should().Be(3, "completing one account import should allow the next account to be scheduled without waiting for the whole account batch");

        accountTasks[1].SetResult(new AccountProcessingResult { AccountId = 2, Success = true });
        accountTasks[2].SetResult(new AccountProcessingResult { AccountId = 3, Success = true });

        var result = await orchestrationTask;

        result.TotalCommandsPublished.Should().Be(3);
        result.PeriodEndId.Should().Be(createdPeriodEnd.Id.ToString());
    }

    [Test]
    public async Task Then_Accounts_Only_Orchestrator_Should_Not_Create_Period_End_Again()
    {
        var correlationId = Guid.NewGuid().ToString();
        var inputPeriodEnd = CreatePeriodEnd("2425-R12", 101);
        var input = new ProcessPeriodEndOrchestratorInput
        {
            CorrelationId = correlationId,
            PeriodEnd = inputPeriodEnd,
            MaxConcurrentAccounts = 2
        };

        _contextMock.Setup(c => c.GetInput<ProcessPeriodEndOrchestratorInput>()).Returns(input);
        _contextMock.SetupGet(c => c.CurrentUtcDateTime).Returns(new DateTime(2026, 4, 22, 12, 0, 0, DateTimeKind.Utc));
        _contextMock.Setup(c => c.CallActivityAsync<List<Accounts>>(
                It.Is<TaskName>(name => name.Name == nameof(ProcessPeriodEndOrchestrator.GetAccountsPageActivity)),
                It.IsAny<GetAccountsRequest>(),
                It.IsAny<TaskOptions>()))
            .ReturnsAsync(new List<Accounts>());

        var result = await _orchestrator.ProcessPeriodEndAccountsOrchestrator(_contextMock.Object);

        result.TotalCommandsPublished.Should().Be(0);
        result.PeriodEndId.Should().Be(inputPeriodEnd.Id.ToString());
        _contextMock.Verify(c => c.CallActivityAsync<PeriodEnd>(
            It.Is<TaskName>(name => name.Name == nameof(ProcessPeriodEndOrchestrator.CreatePeriodEndActivity)),
            It.IsAny<CreatePeriodEndActivityInput>(),
            It.IsAny<TaskOptions>()), Times.Never);
    }

    private static PeriodEnd CreatePeriodEnd(string periodEndId, int id = 0)
    {
        return new PeriodEnd
        {
            Id = id,
            PeriodEndId = periodEndId,
            CalendarPeriodMonth = 12,
            CalendarPeriodYear = 2024,
            AccountDataValidAt = new DateTime(2024, 12, 1, 0, 0, 0, DateTimeKind.Utc),
            CommitmentDataValidAt = new DateTime(2024, 12, 1, 0, 0, 0, DateTimeKind.Utc),
            PaymentsForPeriod = "test"
        };
    }
}
