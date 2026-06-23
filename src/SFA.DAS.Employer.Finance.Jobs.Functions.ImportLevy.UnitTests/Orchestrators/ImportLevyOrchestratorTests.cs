using FluentAssertions;
using HMRC.ESFA.Levy.Api.Types;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Activities;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Orchestrators;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.UnitTests.Orchestrators;

[TestFixture]
public class ImportLevyOrchestratorTests
{
    private Mock<ILogger<ImportLevyOrchestrator>> _logger = null!;
    private Mock<ILogger> _replaySafeLogger = null!;
    private Mock<TaskOrchestrationContext> _context = null!;
    private ImportLevyOrchestrator _orchestrator = null!;

    [SetUp]
    public void SetUp()
    {
        _logger = new Mock<ILogger<ImportLevyOrchestrator>>();
        _replaySafeLogger = new Mock<ILogger>();
        _context = new Mock<TaskOrchestrationContext>();
        _orchestrator = new ImportLevyOrchestrator(_logger.Object);

        _context.Setup(c => c.CreateReplaySafeLogger(It.IsAny<string>())).Returns(_replaySafeLogger.Object);
    }

    [Test]
    public async Task RunOrchestrator_Returns_Success_And_Imports_Levy_Declarations_For_Each_Paye_Scheme()
    {
        var input = new ImportLevyInput
        {
            CorrelationId = "corr-123",
            TriggeredAt = DateTime.UtcNow
        };
        var accountIds = new List<long> { 10, 20 };
        var payeSchemesFor10 = new List<PayeScheme>
        {
            new() { Reference = "123/AB456" },
            new() { Reference = "123/CD789" }
        };
        var payeSchemesFor20 = new List<PayeScheme>
        {
            new() { Reference = "222/XY123", LastSubmissionDate = new DateTime(2026, 1, 1) }
        };

        _context.Setup(c => c.GetInput<ImportLevyInput>()).Returns(input);
        _context.Setup(c => c.CallActivityAsync<List<long>>(It.IsAny<TaskName>(), It.IsAny<string>(), It.IsAny<TaskOptions>()))
            .ReturnsAsync(accountIds);
        _context.Setup(c => c.CallActivityAsync<List<PayeScheme>>(
                It.Is<TaskName>(x => x.Name == nameof(GetAccountPayeSchemesActivity)),
                It.Is<GetAccountPayeSchemesActivityInput>(x => x.AccountId == 10 && x.CorrelationId == "corr-123"),
                It.IsAny<TaskOptions>()))
            .ReturnsAsync(payeSchemesFor10);
        _context.Setup(c => c.CallActivityAsync<List<PayeScheme>>(
                It.Is<TaskName>(x => x.Name == nameof(GetAccountPayeSchemesActivity)),
                It.Is<GetAccountPayeSchemesActivityInput>(x => x.AccountId == 20 && x.CorrelationId == "corr-123"),
                It.IsAny<TaskOptions>()))
            .ReturnsAsync(payeSchemesFor20);
        _context.Setup(c => c.CallActivityAsync<PayeScheme>(
                It.Is<TaskName>(x => x.Name == nameof(GetLevyDeclarationLastSubmissionDateActivity)),
                It.IsAny<GetLevyDeclarationLastSubmissionDateActivityRequest>(),
                It.IsAny<TaskOptions>()))
            .ReturnsAsync((TaskName _, GetLevyDeclarationLastSubmissionDateActivityRequest request, TaskOptions _) =>
                new PayeScheme { Reference = request.EmpRef, LastSubmissionDate = new DateTime(2026, 2, 1) });
        _context.Setup(c => c.CallActivityAsync<ImportLevyDeclarationsActivityResult>(
                It.Is<TaskName>(x => x.Name == nameof(ImportLevyDeclarationsActivity)),
                It.IsAny<ImportLevyActivityRequest>(),
                It.IsAny<TaskOptions>()))
            .ReturnsAsync((TaskName _, ImportLevyActivityRequest request, TaskOptions _) =>
                new ImportLevyDeclarationsActivityResult(request.EmpRef, request.FromDate, 2, new LevyDeclarations()));

        var result = await _orchestrator.RunOrchestrator(_context.Object);

        result.Success.Should().BeTrue();
        result.CorrelationId.Should().Be("corr-123");
        result.TotalAccountsCount.Should().Be(2);
        result.TotalPayeSchemesCount.Should().Be(3);
        result.AccountsWithoutPayeSchemesCount.Should().Be(0);
        result.LevyDeclarationsActivityResults.Should().HaveCount(3);
        result.LevyDeclarationsActivityResults.Sum(x => x.DeclarationsCount).Should().Be(6);
    }

    [Test]
    public async Task RunOrchestrator_Returns_Success_With_Empty_List_When_Accounts_Activity_Returns_Null()
    {
        var input = new ImportLevyInput
        {
            CorrelationId = "corr-456",
            TriggeredAt = DateTime.UtcNow
        };

        _context.Setup(c => c.GetInput<ImportLevyInput>()).Returns(input);
        _context.Setup(c => c.CallActivityAsync<List<long>>(It.IsAny<TaskName>(), It.IsAny<string>(), It.IsAny<TaskOptions>()))
            .Returns(Task.FromResult((List<long>)null!));

        var result = await _orchestrator.RunOrchestrator(_context.Object);

        result.Success.Should().BeTrue();
        result.CorrelationId.Should().Be("corr-456");
        result.TotalAccountsCount.Should().Be(0);
        result.TotalPayeSchemesCount.Should().Be(0);
        result.AccountIds.Should().BeEmpty();
    }

    [Test]
    public async Task RunOrchestrator_Continues_When_Account_Has_No_Paye_Schemes()
    {
        var input = new ImportLevyInput
        {
            CorrelationId = "corr-654",
            TriggeredAt = DateTime.UtcNow
        };

        _context.Setup(c => c.GetInput<ImportLevyInput>()).Returns(input);
        _context.Setup(c => c.CallActivityAsync<List<long>>(It.IsAny<TaskName>(), It.IsAny<string>(), It.IsAny<TaskOptions>()))
            .ReturnsAsync(new List<long> { 99 });
        _context.Setup(c => c.CallActivityAsync<List<PayeScheme>>(
                It.Is<TaskName>(x => x.Name == nameof(GetAccountPayeSchemesActivity)),
                It.Is<GetAccountPayeSchemesActivityInput>(x => x.AccountId == 99),
                It.IsAny<TaskOptions>()))
            .ReturnsAsync(new List<PayeScheme>());

        var result = await _orchestrator.RunOrchestrator(_context.Object);

        result.Success.Should().BeTrue();
        result.TotalAccountsCount.Should().Be(1);
        result.TotalPayeSchemesCount.Should().Be(0);
        result.AccountsWithoutPayeSchemesCount.Should().Be(1);

        _context.Verify(c => c.CallActivityAsync<ImportLevyDeclarationsActivityResult>(
                It.Is<TaskName>(x => x.Name == nameof(ImportLevyDeclarationsActivity)),
                It.IsAny<ImportLevyActivityRequest>(),
                It.IsAny<TaskOptions>()),
            Times.Never);
    }

    [Test]
    public async Task RunOrchestrator_Returns_Failure_When_Activity_Throws()
    {
        var input = new ImportLevyInput
        {
            CorrelationId = "corr-789",
            TriggeredAt = DateTime.UtcNow
        };

        _context.Setup(c => c.GetInput<ImportLevyInput>()).Returns(input);
        _context.Setup(c => c.CallActivityAsync<List<long>>(It.IsAny<TaskName>(), It.IsAny<string>(), It.IsAny<TaskOptions>()))
            .ThrowsAsync(new Exception("activity failed"));

        var result = await _orchestrator.RunOrchestrator(_context.Object);

        result.Success.Should().BeFalse();
        result.CorrelationId.Should().Be("corr-789");
        result.ErrorMessage.Should().Be("activity failed");
    }

    [Test]
    public async Task RunOrchestrator_Uses_New_Guid_When_Input_Is_Missing()
    {
        var generatedCorrelationId = Guid.NewGuid();

        _context.Setup(c => c.GetInput<ImportLevyInput>()).Returns((ImportLevyInput?)null);
        _context.Setup(c => c.NewGuid()).Returns(generatedCorrelationId);
        _context.Setup(c => c.CallActivityAsync<List<long>>(It.IsAny<TaskName>(), It.IsAny<string>(), It.IsAny<TaskOptions>()))
            .ReturnsAsync(new List<long>());

        var result = await _orchestrator.RunOrchestrator(_context.Object);

        result.Success.Should().BeTrue();
        result.CorrelationId.Should().Be(generatedCorrelationId.ToString());
    }
}
