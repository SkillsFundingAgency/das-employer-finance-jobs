using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using SFA.DAS.Employer.Finance.Jobs.Functions.Orchestrators;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;


namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Orchestrator;

[TestFixture]
public class ProcessAccountOrchestratorTests
{  

    [Test]
    public async Task RunOrchestrator_WithInput_ReturnsResultMappedFromInput()
    {
        // Arrange
        var input = new ProcessAccountInput
        {
            AccountId = 1234,
            CorrelationId = "corr-1234"
        };

        var mockContext = new Mock<TaskOrchestrationContext>();
        mockContext
            .Setup(c => c.GetInput<ProcessAccountInput>())
            .Returns(input);

        var orchestrator = new ProcessAccountOrchestrator(NullLogger<ProcessAccountOrchestrator>.Instance);

        // Act
        var result = await orchestrator.RunOrchestrator(mockContext.Object);

        // Assert
        result.Should().NotBeNull();
        result.AccountId.Should().Be(input.AccountId);
        result.Success.Should().BeTrue();
        result.PaymentsProcessed.Should().Be(0);
        result.TransfersProcessed.Should().Be(0);
    }

    [Test]
    public async Task RunOrchestrator_WithNullInput_ReturnsDefaultResult()
    {
        // Arrange
        var mockContext = new Mock<TaskOrchestrationContext>();
        mockContext
            .Setup(c => c.GetInput<ProcessAccountInput>())
            .Returns((ProcessAccountInput?)null);

        var orchestrator = new ProcessAccountOrchestrator(NullLogger<ProcessAccountOrchestrator>.Instance);

        // Act
        var result = await orchestrator.RunOrchestrator(mockContext.Object);

        // Assert
        result.Should().NotBeNull();
        result.AccountId.Should().Be(0);
        result.Success.Should().BeTrue();
        result.PaymentsProcessed.Should().Be(0);
        result.TransfersProcessed.Should().Be(0);
    }
}