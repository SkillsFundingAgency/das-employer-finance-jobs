using FluentAssertions;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Orchestrators;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.UnitTests.Orchestrators;

[TestFixture]
public class ImportLevyOrchestratorTests
{
    private Mock<ILogger<ImportLevyOrchestrator>> _logger = null!;
    private Mock<TaskOrchestrationContext> _context = null!;
    private ImportLevyOrchestrator _orchestrator = null!;

    [SetUp]
    public void SetUp()
    {
        _logger = new Mock<ILogger<ImportLevyOrchestrator>>();
        _context = new Mock<TaskOrchestrationContext>();
        _orchestrator = new ImportLevyOrchestrator(_logger.Object);
    }

    [Test]
    public async Task RunOrchestrator_Returns_Success_When_Accounts_Are_Retrieved()
    {
        var input = new ImportLevyInput
        {
            CorrelationId = "corr-123",
            TriggeredAt = DateTime.UtcNow
        };
        var accountIds = new List<long> { 10, 20, 30 };

        _context.Setup(c => c.GetInput<ImportLevyInput>()).Returns(input);
        _context.Setup(c => c.CallActivityAsync<List<long>>(It.IsAny<TaskName>(), It.IsAny<string>(), It.IsAny<TaskOptions>()))
            .ReturnsAsync(accountIds);

        var result = await _orchestrator.RunOrchestrator(_context.Object);

        result.Success.Should().BeTrue();
        result.CorrelationId.Should().Be("corr-123");
        result.TotalAccountsCount.Should().Be(3);
        result.AccountIds.Should().Equal(accountIds);
        result.ErrorMessage.Should().BeEmpty();
    }

    [Test]
    public async Task RunOrchestrator_Returns_Success_With_Empty_List_When_Activity_Returns_Null()
    {
        var input = new ImportLevyInput
        {
            CorrelationId = "corr-456",
            TriggeredAt = DateTime.UtcNow
        };

        _context.Setup(c => c.GetInput<ImportLevyInput>()).Returns(input);
        _context.Setup(c => c.CallActivityAsync<List<long>>(It.IsAny<TaskName>(), It.IsAny<string>(), It.IsAny<TaskOptions>()))
            .ReturnsAsync((List<long>?)null);

        var result = await _orchestrator.RunOrchestrator(_context.Object);

        result.Success.Should().BeTrue();
        result.CorrelationId.Should().Be("corr-456");
        result.TotalAccountsCount.Should().Be(0);
        result.AccountIds.Should().BeEmpty();
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
}
