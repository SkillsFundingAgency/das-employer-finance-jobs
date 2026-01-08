using FluentAssertions;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Orchestrators;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Orchestrators.ProcessPeriodEndOrchestratorTests;

[TestFixture]
public class WhenRunningProcessPeriodEndOrchestrator
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

        _contextMock
            .Setup(c => c.CurrentUtcDateTime)
            .Returns(DateTime.UtcNow);

        _contextMock
            .Setup(c => c.NewGuid())
            .Returns(Guid.NewGuid());

        _orchestrator = new ProcessPeriodEndOrchestrator(
            _loggerMock.Object,
            _periodEndServiceMock.Object,
            _accountServiceMock.Object);
    }

    [Test]
    public void Then_Can_Be_Instantiated()
    {
        _orchestrator.Should().NotBeNull();
    }

    [Test]
    public async Task Then_Returns_Result_When_Single_Page_Of_Accounts()
    {
        var input = CreateValidPeriodEnd("PE-202401");

        _contextMock.Setup(c => c.GetInput<PeriodEnd>())
            .Returns(input);

        _periodEndServiceMock
            .Setup(s => s.CreatePeriodEndAsync(input, It.IsAny<Guid>()))
            .ReturnsAsync(new PeriodEnd { Id = 123 });

        _accountServiceMock
            .Setup(s => s.GetAccountsAsync(It.Is<GetAccountsRequest>(r => r.Page == 1)))
            .ReturnsAsync(new List<Accounts>
            {
                new Accounts { Id = 1, Name = "Account 1" },
                new Accounts { Id = 2, Name = "Account 2" }
            });

        var result = await _orchestrator.Run(_contextMock.Object);

        result.Should().NotBeNull();
        result.PeriodEndId.Should().Be("123");
        result.TotalAccountsRetrieved.Should().Be(2);
    }

    [Test]
    public async Task Then_Pages_Until_No_More_Accounts()
    {
        var input = CreateValidPeriodEnd("PE-PAGED");

        _contextMock.Setup(c => c.GetInput<PeriodEnd>())
            .Returns(input);

        _periodEndServiceMock
            .Setup(s => s.CreatePeriodEndAsync(input, It.IsAny<Guid>()))
            .ReturnsAsync(new PeriodEnd { Id = 456 });

        _accountServiceMock
            .Setup(s => s.GetAccountsAsync(It.Is<GetAccountsRequest>(r => r.Page == 1)))
            .ReturnsAsync(CreateAccounts(10000));

        _accountServiceMock
            .Setup(s => s.GetAccountsAsync(It.Is<GetAccountsRequest>(r => r.Page == 2)))
            .ReturnsAsync(CreateAccounts(5));

        var result = await _orchestrator.Run(_contextMock.Object);

        result.PeriodEndId.Should().Be("456");
        result.TotalAccountsRetrieved.Should().Be(10005);

        _accountServiceMock.Verify(
            s => s.GetAccountsAsync(It.IsAny<GetAccountsRequest>()),
            Times.Exactly(2));
    }

    [Test]
    public async Task Then_Returns_Zero_When_No_Accounts_Returned()
    {
        var input = CreateValidPeriodEnd("PE-EMPTY");

        _contextMock.Setup(c => c.GetInput<PeriodEnd>())
            .Returns(input);

        _periodEndServiceMock
            .Setup(s => s.CreatePeriodEndAsync(input, It.IsAny<Guid>()))
            .ReturnsAsync(new PeriodEnd { Id = 999 });

        _accountServiceMock
            .Setup(s => s.GetAccountsAsync(It.IsAny<GetAccountsRequest>()))
            .ReturnsAsync(new List<Accounts>());

        var result = await _orchestrator.Run(_contextMock.Object);

        result.TotalAccountsRetrieved.Should().Be(0);
    }

    [Test]
    public void Then_Throws_When_AccountDataValidAt_Is_Missing()
    {
        var input = new PeriodEnd
        {
            PeriodEndId = "PE-INVALID",
            CommitmentDataValidAt = DateTime.UtcNow.AddMinutes(-1)
        };

        _contextMock.Setup(c => c.GetInput<PeriodEnd>())
            .Returns(input);

        Func<Task> act = () => _orchestrator.Run(_contextMock.Object);

        act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("AccountDataValidAt must be provided.");
    }

    [Test]
    public void Then_Throws_When_CommitmentDataValidAt_Is_Missing()
    {
        var input = new PeriodEnd
        {
            PeriodEndId = "PE-INVALID",
            AccountDataValidAt = DateTime.UtcNow.AddMinutes(-1)
        };

        _contextMock.Setup(c => c.GetInput<PeriodEnd>())
            .Returns(input);

        Func<Task> act = () => _orchestrator.Run(_contextMock.Object);

        act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CommitmentDataValidAt must be provided.");
    }

    [Test]
    public void Then_Throws_When_Dates_Are_In_The_Future()
    {
        var input = new PeriodEnd
        {
            PeriodEndId = "PE-FUTURE",
            AccountDataValidAt = DateTime.UtcNow.AddMinutes(10),
            CommitmentDataValidAt = DateTime.UtcNow.AddMinutes(10)
        };

        _contextMock.Setup(c => c.GetInput<PeriodEnd>())
            .Returns(input);

        Func<Task> act = () => _orchestrator.Run(_contextMock.Object);

        act.Should().ThrowAsync<InvalidOperationException>();
    }

    private static PeriodEnd CreateValidPeriodEnd(string periodEndId)
    {
        return new PeriodEnd
        {
            PeriodEndId = periodEndId,
            AccountDataValidAt = DateTime.UtcNow.AddMinutes(-1),
            CommitmentDataValidAt = DateTime.UtcNow.AddMinutes(-1)
        };
    }

    private static List<Accounts> CreateAccounts(int count)
    {
        var list = new List<Accounts>();

        for (var i = 0; i < count; i++)
        {
            list.Add(new Accounts
            {
                Id = i + 1,
                Name = $"Account {i + 1}"
            });
        }

        return list;
    }
}
