using System.Collections.Generic;
using System.Threading;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NServiceBus;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Messages.Commands;
using SFA.DAS.Employer.Finance.Jobs.Orchestrators;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Orchestrators.ProcessPeriodEndOrchestratorTests;

public class WhenCallingPublishAccountPaymentCommandsActivity
{
    private Mock<ILogger<ProcessPeriodEndOrchestrator>> _mockLogger;
    private Mock<IPeriodEndService> _mockPeriodEndService;
    private Mock<IAccountService> _mockAccountService;
    private Mock<IFunctionEndpoint> _mockFunctionEndpoint;
    private Mock<FunctionContext> _mockFunctionContext;
    private ProcessPeriodEndOrchestrator _orchestrator;

    [SetUp]
    public void SetUp()
    {
        _mockLogger = new Mock<ILogger<ProcessPeriodEndOrchestrator>>();
        _mockPeriodEndService = new Mock<IPeriodEndService>();
        _mockAccountService = new Mock<IAccountService>();
        _mockFunctionEndpoint = new Mock<IFunctionEndpoint>();
        _mockFunctionContext = new Mock<FunctionContext>();
        _orchestrator = new ProcessPeriodEndOrchestrator(
            _mockLogger.Object,
            _mockPeriodEndService.Object,
            _mockAccountService.Object,
            _mockFunctionEndpoint.Object);
    }

    [Test]
    public async Task Then_Publishes_ImportAccountPaymentsCommand_Per_Account()
    {
        // Arrange
        var periodEndRef = "PE-202401";
        var input = new PublishAccountPaymentCommandsInput
        {
            PeriodEndRef = periodEndRef,
            PeriodEnd = new PeriodEnd { Id = 123, PeriodEndId = periodEndRef }
        };
        var accounts = new List<Accounts>
        {
            new() { Id = 1, Name = "Account 1" },
            new() { Id = 2, Name = "Account 2" }
        };

        _mockAccountService
            .Setup(x => x.GetAccountsAsync(It.Is<GetAccountsRequest>(r => r.Page == 1 && r.PageSize == 10000)))
            .ReturnsAsync(accounts);

        // Act
        var result = await _orchestrator.PublishAccountPaymentCommandsActivity(
            input,
            _mockFunctionContext.Object,
            CancellationToken.None);

        // Assert
        result.Should().Be(2);
        _mockFunctionEndpoint.Verify(
            x => x.Send(
                It.Is<ImportAccountPaymentsCommand>(c => c.AccountId == 1 && c.PeriodEndRef == periodEndRef),
                It.IsAny<SendOptions>(),
                It.IsAny<FunctionContext>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _mockFunctionEndpoint.Verify(
            x => x.Send(
                It.Is<ImportAccountPaymentsCommand>(c => c.AccountId == 2 && c.PeriodEndRef == periodEndRef),
                It.IsAny<SendOptions>(),
                It.IsAny<FunctionContext>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task And_No_Accounts_Returned_Then_Returns_Zero()
    {
        // Arrange
        var periodEndRef = "PE-EMPTY";
        var input = new PublishAccountPaymentCommandsInput
        {
            PeriodEndRef = periodEndRef,
            PeriodEnd = new PeriodEnd { Id = 999, PeriodEndId = periodEndRef }
        };

        _mockAccountService
            .Setup(x => x.GetAccountsAsync(It.IsAny<GetAccountsRequest>()))
            .ReturnsAsync(new List<Accounts>());

        // Act
        var result = await _orchestrator.PublishAccountPaymentCommandsActivity(
            input,
            _mockFunctionContext.Object,
            CancellationToken.None);

        // Assert
        result.Should().Be(0);
        _mockFunctionEndpoint.VerifyNoOtherCalls();
    }

    [Test]
    public async Task And_Null_Accounts_Returned_Then_Returns_Zero()
    {
        // Arrange
        var periodEndRef = "PE-NULL";
        var input = new PublishAccountPaymentCommandsInput
        {
            PeriodEndRef = periodEndRef,
            PeriodEnd = new PeriodEnd { Id = 888, PeriodEndId = periodEndRef }
        };

        _mockAccountService
            .Setup(x => x.GetAccountsAsync(It.IsAny<GetAccountsRequest>()))
            .ReturnsAsync((List<Accounts>)null);

        // Act
        var result = await _orchestrator.PublishAccountPaymentCommandsActivity(
            input,
            _mockFunctionContext.Object,
            CancellationToken.None);

        // Assert
        result.Should().Be(0);
        _mockFunctionEndpoint.VerifyNoOtherCalls();
    }

    [Test]
    public async Task And_Multiple_Pages_Then_Publishes_All_Accounts()
    {
        // Arrange
        var periodEndRef = "PE-PAGED";
        var input = new PublishAccountPaymentCommandsInput
        {
            PeriodEndRef = periodEndRef,
            PeriodEnd = new PeriodEnd { Id = 456, PeriodEndId = periodEndRef }
        };
        var page1Accounts = new List<Accounts>();
        for (var i = 0; i < 10000; i++)
            page1Accounts.Add(new Accounts { Id = i + 1, Name = $"Account {i + 1}" });
        var page2Accounts = new List<Accounts>
        {
            new() { Id = 10001, Name = "Account 10001" },
            new() { Id = 10002, Name = "Account 10002" }
        };

        _mockAccountService
            .Setup(x => x.GetAccountsAsync(It.Is<GetAccountsRequest>(r => r.Page == 1)))
            .ReturnsAsync(page1Accounts);
        _mockAccountService
            .Setup(x => x.GetAccountsAsync(It.Is<GetAccountsRequest>(r => r.Page == 2)))
            .ReturnsAsync(page2Accounts);

        // Act
        var result = await _orchestrator.PublishAccountPaymentCommandsActivity(
            input,
            _mockFunctionContext.Object,
            CancellationToken.None);

        // Assert
        result.Should().Be(10002);
        _mockAccountService.VerifyAll();
        _mockFunctionEndpoint.Verify(
            x => x.Send(It.IsAny<ImportAccountPaymentsCommand>(), It.IsAny<SendOptions>(), It.IsAny<FunctionContext>(), It.IsAny<CancellationToken>()),
            Times.Exactly(10002));
    }

    [Test]
    public async Task And_AccountService_Throws_Then_Throws_Exception()
    {
        // Arrange
        var input = new PublishAccountPaymentCommandsInput
        {
            PeriodEndRef = "PE-ERR",
            PeriodEnd = new PeriodEnd { Id = 1, PeriodEndId = "PE-ERR" }
        };
        var expectedException = new InvalidOperationException("Finance API failure");

        _mockAccountService
            .Setup(x => x.GetAccountsAsync(It.IsAny<GetAccountsRequest>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var act = async () => await _orchestrator.PublishAccountPaymentCommandsActivity(
            input,
            _mockFunctionContext.Object,
            CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Finance API failure");
    }
}
