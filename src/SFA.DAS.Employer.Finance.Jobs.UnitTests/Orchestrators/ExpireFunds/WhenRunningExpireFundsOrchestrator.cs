using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Functions.ExpireFunds.Activities;
using SFA.DAS.Employer.Finance.Jobs.Functions.ExpireFunds.Orchestrators;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;
using SFA.DAS.Employer.Finance.Jobs.UnitTests.Helpers;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Orchestrators.ExpireFunds;

[TestFixture]
public class WhenRunningExpireFundsOrchestrator
{
    private Mock<ILogger<ExpireFundsOrchestrator>> _loggerMock;
    private Mock<TaskOrchestrationContext> _contextMock;
    private ExpireFundsOrchestrator _orchestrator;

    [SetUp]
    public void SetUp()
    {
        _loggerMock = new Mock<ILogger<ExpireFundsOrchestrator>>();
        _contextMock = new Mock<TaskOrchestrationContext>();
        _orchestrator = new ExpireFundsOrchestrator(_loggerMock.Object);
    }

    [Test]
    public async Task Then_All_Account_Pages_Are_Processed_And_A_Final_Summary_Is_Returned()
    {
        var correlationId = Guid.NewGuid().ToString();
        var accountPageRequests = new List<GetAccountsRequest>();
        var pages = new Dictionary<int, List<Accounts>>
        {
            [1] = [new Accounts { Id = 1, Name = "A1" }, new Accounts { Id = 2, Name = "A2" }],
            [2] = [new Accounts { Id = 3, Name = "A3" }, new Accounts { Id = 4, Name = "A4" }],
            [3] = []
        };

        SetUpInput(correlationId, accountPageSize: 2, maxConcurrentAccounts: 2);
        _contextMock
            .Setup(context => context.CallActivityAsync<List<Accounts>>(
                It.Is<TaskName>(name => name.Name == nameof(ExpireFundsActivities.GetAccountsPageActivity)),
                It.IsAny<GetAccountsRequest>(),
                It.IsAny<TaskOptions>()))
            .Returns((TaskName _, GetAccountsRequest request, TaskOptions _) =>
            {
                accountPageRequests.Add(request);
                return Task.FromResult(pages[request.Page]);
            });
        _contextMock
            .Setup(context => context.CallActivityAsync<ProcessAccountExpireFundsResult>(
                It.Is<TaskName>(name => name.Name == ExpireFundsOrchestrator.ProcessAccountActivityName),
                It.IsAny<ProcessAccountExpireFundsInput>(),
                It.IsAny<TaskOptions>()))
            .Returns((TaskName _, ProcessAccountExpireFundsInput input, TaskOptions _) =>
                Task.FromResult(new ProcessAccountExpireFundsResult
                {
                    AccountId = input.AccountId,
                    Success = true,
                    FundsExpired = input.AccountId != 2
                }));

        var result = await _orchestrator.RunOrchestrator(_contextMock.Object);

        accountPageRequests.Select(request => request.Page).Should().Equal(1, 2, 3);
        accountPageRequests.Should().OnlyContain(request =>
            request.PageSize == 2 && request.CorrelationId == correlationId);
        result.Success.Should().BeTrue();
        result.CorrelationId.Should().Be(correlationId);
        result.PagesProcessed.Should().Be(3);
        result.TotalAccountsCount.Should().Be(4);
        result.ProcessedAccountsCount.Should().Be(4);
        result.SuccessfulAccountsCount.Should().Be(4);
        result.FailedAccountsCount.Should().Be(0);
        result.FundsExpiredAccountsCount.Should().Be(3);
        _loggerMock.VerifyLogContains(LogLevel.Information, "ExpireFundsOrchestrator completed");
        _loggerMock.VerifyLogContains(LogLevel.Information, correlationId);
    }

    [Test]
    public async Task Then_Account_Processing_Uses_The_Durable_Retry_Policy_And_Success_Is_Recorded()
    {
        TaskOptions capturedOptions = null!;
        var correlationId = Guid.NewGuid().ToString();

        SetUpInput(correlationId, accountPageSize: 10, maxConcurrentAccounts: 1);
        _contextMock
            .Setup(context => context.CallActivityAsync<List<Accounts>>(
                It.Is<TaskName>(name => name.Name == nameof(ExpireFundsActivities.GetAccountsPageActivity)),
                It.IsAny<GetAccountsRequest>(),
                It.IsAny<TaskOptions>()))
            .ReturnsAsync([new Accounts { Id = 12345, Name = "Test account" }]);
        _contextMock
            .Setup(context => context.CallActivityAsync<ProcessAccountExpireFundsResult>(
                It.Is<TaskName>(name => name.Name == ExpireFundsOrchestrator.ProcessAccountActivityName),
                It.IsAny<ProcessAccountExpireFundsInput>(),
                It.IsAny<TaskOptions>()))
            .Returns((TaskName _, ProcessAccountExpireFundsInput input, TaskOptions options) =>
            {
                capturedOptions = options;
                return Task.FromResult(new ProcessAccountExpireFundsResult
                {
                    AccountId = input.AccountId,
                    Success = true,
                    FundsExpired = true
                });
            });

        var result = await _orchestrator.RunOrchestrator(_contextMock.Object);

        capturedOptions.Retry.Policy.MaxNumberOfAttempts.Should().Be(3);
        capturedOptions.Retry.Policy.FirstRetryInterval.Should().Be(TimeSpan.FromSeconds(5));
        result.Success.Should().BeTrue();
        result.SuccessfulAccountsCount.Should().Be(1);
        result.FailedAccountsCount.Should().Be(0);
        result.FundsExpiredAccountsCount.Should().Be(1);
    }

    [Test]
    public async Task Then_Durable_Retry_Exhaustion_Is_Captured_As_An_Account_Level_Failure()
    {
        TaskOptions capturedOptions = null!;
        var correlationId = Guid.NewGuid().ToString();

        SetUpInput(correlationId, accountPageSize: 10, maxConcurrentAccounts: 1);
        _contextMock
            .Setup(context => context.CallActivityAsync<List<Accounts>>(
                It.Is<TaskName>(name => name.Name == nameof(ExpireFundsActivities.GetAccountsPageActivity)),
                It.IsAny<GetAccountsRequest>(),
                It.IsAny<TaskOptions>()))
            .ReturnsAsync([new Accounts { Id = 12345, Name = "Test account" }]);
        _contextMock
            .Setup(context => context.CallActivityAsync<ProcessAccountExpireFundsResult>(
                It.Is<TaskName>(name => name.Name == ExpireFundsOrchestrator.ProcessAccountActivityName),
                It.IsAny<ProcessAccountExpireFundsInput>(),
                It.IsAny<TaskOptions>()))
            .Returns((TaskName _, ProcessAccountExpireFundsInput _, TaskOptions options) =>
            {
                capturedOptions = options;
                return Task.FromException<ProcessAccountExpireFundsResult>(
                    new TimeoutException("Finance API remained unavailable after retries"));
            });

        var result = await _orchestrator.RunOrchestrator(_contextMock.Object);

        capturedOptions.Retry.Policy.MaxNumberOfAttempts.Should().Be(3);
        capturedOptions.Retry.Policy.FirstRetryInterval.Should().Be(TimeSpan.FromSeconds(5));
        result.Success.Should().BeFalse();
        result.ProcessedAccountsCount.Should().Be(1);
        result.SuccessfulAccountsCount.Should().Be(0);
        result.FailedAccountsCount.Should().Be(1);
        _loggerMock.VerifyLogContains(LogLevel.Error, "Continuing with remaining accounts");
        _loggerMock.VerifyLogContains(LogLevel.Error, "AccountId 12345");
        _loggerMock.VerifyLogContains(LogLevel.Error, correlationId);
    }

    [Test]
    public async Task Then_An_Empty_First_Page_Completes_Without_Scheduling_Accounts()
    {
        SetUpInput(Guid.NewGuid().ToString(), accountPageSize: 100, maxConcurrentAccounts: 10);
        _contextMock
            .Setup(context => context.CallActivityAsync<List<Accounts>>(
                It.Is<TaskName>(name => name.Name == nameof(ExpireFundsActivities.GetAccountsPageActivity)),
                It.Is<GetAccountsRequest>(request => request.Page == 1),
                It.IsAny<TaskOptions>()))
            .ReturnsAsync([]);

        var result = await _orchestrator.RunOrchestrator(_contextMock.Object);

        result.Success.Should().BeTrue();
        result.PagesProcessed.Should().Be(1);
        result.TotalAccountsCount.Should().Be(0);
        result.ProcessedAccountsCount.Should().Be(0);
        _contextMock.Verify(context => context.CallActivityAsync<ProcessAccountExpireFundsResult>(
            It.IsAny<TaskName>(),
            It.IsAny<ProcessAccountExpireFundsInput>(),
            It.IsAny<TaskOptions>()), Times.Never);
    }

    [Test]
    public async Task Then_Account_Processing_Is_Throttled_And_One_Failure_Does_Not_Stop_Remaining_Accounts()
    {
        var correlationId = Guid.NewGuid().ToString();
        var accounts = new List<Accounts>
        {
            new() { Id = 1, Name = "A1" },
            new() { Id = 2, Name = "A2" },
            new() { Id = 3, Name = "A3" }
        };
        var accountTasks = Enumerable.Range(0, accounts.Count)
            .Select(_ => new TaskCompletionSource<ProcessAccountExpireFundsResult>(TaskCreationOptions.RunContinuationsAsynchronously))
            .ToList();
        var scheduledCount = 0;

        SetUpInput(correlationId, accountPageSize: 10, maxConcurrentAccounts: 2);
        _contextMock
            .Setup(context => context.CallActivityAsync<List<Accounts>>(
                It.IsAny<TaskName>(),
                It.IsAny<GetAccountsRequest>(),
                It.IsAny<TaskOptions>()))
            .ReturnsAsync(accounts);
        _contextMock
            .Setup(context => context.CallActivityAsync<ProcessAccountExpireFundsResult>(
                It.Is<TaskName>(name => name.Name == ExpireFundsOrchestrator.ProcessAccountActivityName),
                It.IsAny<ProcessAccountExpireFundsInput>(),
                It.IsAny<TaskOptions>()))
            .Returns(() => accountTasks[Interlocked.Increment(ref scheduledCount) - 1].Task);

        var orchestrationTask = _orchestrator.RunOrchestrator(_contextMock.Object);

        await WaitUntilAsync(() => Volatile.Read(ref scheduledCount) == 2);
        scheduledCount.Should().Be(2, "the concurrency window should be full before a third account is scheduled");

        accountTasks[0].SetException(new InvalidOperationException("Account 1 failed"));

        await WaitUntilAsync(() => Volatile.Read(ref scheduledCount) == 3);
        scheduledCount.Should().Be(3, "a failed account should release capacity for the next account");

        accountTasks[1].SetResult(new ProcessAccountExpireFundsResult
        {
            AccountId = 2,
            Success = true,
            FundsExpired = false
        });
        accountTasks[2].SetResult(new ProcessAccountExpireFundsResult
        {
            AccountId = 3,
            Success = true,
            FundsExpired = true
        });

        var result = await orchestrationTask;

        result.Success.Should().BeFalse();
        result.TotalAccountsCount.Should().Be(3);
        result.ProcessedAccountsCount.Should().Be(3);
        result.SuccessfulAccountsCount.Should().Be(2);
        result.FailedAccountsCount.Should().Be(1);
        result.FundsExpiredAccountsCount.Should().Be(1);
        _contextMock.Verify(context => context.CallActivityAsync<ProcessAccountExpireFundsResult>(
            It.Is<TaskName>(name => name.Name == ExpireFundsOrchestrator.ProcessAccountActivityName),
            It.IsAny<ProcessAccountExpireFundsInput>(),
            It.IsAny<TaskOptions>()), Times.Exactly(3));
        _loggerMock.VerifyLogContains(LogLevel.Error, "Continuing with remaining accounts");
        _loggerMock.VerifyLogContains(LogLevel.Error, "AccountId 1");
    }

    [Test]
    public async Task Then_A_Page_Retrieval_Failure_Is_Reported_In_The_Final_Summary()
    {
        var correlationId = Guid.NewGuid().ToString();
        SetUpInput(correlationId, accountPageSize: 100, maxConcurrentAccounts: 10);
        _contextMock
            .Setup(context => context.CallActivityAsync<List<Accounts>>(
                It.IsAny<TaskName>(),
                It.IsAny<GetAccountsRequest>(),
                It.IsAny<TaskOptions>()))
            .ThrowsAsync(new InvalidOperationException("Accounts API failed"));

        var result = await _orchestrator.RunOrchestrator(_contextMock.Object);

        result.Success.Should().BeFalse();
        result.CorrelationId.Should().Be(correlationId);
        result.ErrorMessage.Should().Be("Accounts API failed");
        result.PagesProcessed.Should().Be(0);
        result.ProcessedAccountsCount.Should().Be(0);
        _loggerMock.VerifyLogContains(LogLevel.Error, "failed while processing account pages");
        _loggerMock.VerifyLogContains(LogLevel.Information, "ExpireFundsOrchestrator completed");
    }

    private void SetUpInput(string correlationId, int accountPageSize, int maxConcurrentAccounts)
    {
        _contextMock
            .Setup(context => context.GetInput<ExpireFundsOrchestratorInput>())
            .Returns(new ExpireFundsOrchestratorInput
            {
                CorrelationId = correlationId,
                AccountPageSize = accountPageSize,
                MaxConcurrentAccounts = maxConcurrentAccounts
            });
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        while (!condition())
        {
            await Task.Delay(10, timeout.Token);
        }
    }
}
