using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SFA.DAS.Employer.Finance.Jobs.Functions;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces.Services;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Orchestrators;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Orchestrators.ImportPaymentsOrchestrator;

[TestFixture]
public class WhenImportPaymentsOrchestratorRun
{
    private Mock<ILogger<Orchestrators.ImportPaymentsOrchestrator>> _loggerMock;
    private Mock<IPeriodEndService> _periodEndServiceMock;
    private Mock<TaskOrchestrationContext> _contextMock;
    private Orchestrators.ImportPaymentsOrchestrator _orchestrator;

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
        var input = new ImportPaymentsOrchestratorInput { CorrelationId = correlationId, TriggeredAt = DateTime.UtcNow };
        var periodEnds = new List<PeriodEnd>
        {
            new PeriodEnd { PeriodEndId = "PE1", CalendarPeriodYear = 2024, PaymentsForPeriod = "Apr" },
            new PeriodEnd { PeriodEndId = "PE2", CalendarPeriodYear = 2024, PaymentsForPeriod = "May" }
        };

        _contextMock.Setup(c => c.GetInput<ImportPaymentsOrchestratorInput>()).Returns(input);
        _contextMock.Setup(c => c.CallActivityAsync<List<PeriodEnd>>(It.IsAny<TaskName>(), It.IsAny<string>(), It.IsAny<TaskOptions>()))
            .ReturnsAsync(periodEnds);
        _contextMock.Setup(c => c.CallActivityAsync(It.IsAny<TaskName>(), It.IsAny<ProcessPeriodEndInput>(), It.IsAny<TaskOptions>()))
            .Returns(Task.CompletedTask);

        var result = await _orchestrator.RunOrchestrator(_contextMock.Object);

        result.Success.Should().BeTrue();
        result.CorrelationId.Should().Be(correlationId);
        result.NewPeriodEndsCount.Should().Be(periodEnds.Count);
        result.TotalPeriodEndsCount.Should().Be(periodEnds.Count);
        result.ErrorMessage.Should().BeNull();
    }

    [Test]
    public async Task Then_ShouldReturnSuccess_WhenNoPeriodEndsToProcess()
    {
        var correlationId = Guid.NewGuid().ToString();
        var input = new ImportPaymentsOrchestratorInput { CorrelationId = correlationId, TriggeredAt = DateTime.UtcNow };
        var periodEnds = new List<PeriodEnd>();

        _contextMock.Setup(c => c.GetInput<ImportPaymentsOrchestratorInput>()).Returns(input);
        _contextMock.Setup(c => c.CallActivityAsync<List<PeriodEnd>>(It.IsAny<TaskName>(), It.IsAny<string>(), It.IsAny<TaskOptions>()))
            .ReturnsAsync(periodEnds);

        var result = await _orchestrator.RunOrchestrator(_contextMock.Object);

        result.Success.Should().BeTrue();
        result.CorrelationId.Should().Be(correlationId);
        result.NewPeriodEndsCount.Should().Be(0);
        result.TotalPeriodEndsCount.Should().Be(0);
        result.ErrorMessage.Should().BeNull();
    }

    [Test]
    public async Task Then_ShouldReturnFailure_WhenExceptionIsThrown()
    {
        var correlationId = Guid.NewGuid().ToString();
        var input = new ImportPaymentsOrchestratorInput { CorrelationId = correlationId, TriggeredAt = DateTime.UtcNow };

        _contextMock.Setup(c => c.GetInput<ImportPaymentsOrchestratorInput>()).Returns(input);
        _contextMock.Setup(c => c.CallActivityAsync<List<PeriodEnd>>(It.IsAny<TaskName>(), It.IsAny<string>(), It.IsAny<TaskOptions>()))
            .ThrowsAsync(new Exception("Test error"));

        var result = await _orchestrator.RunOrchestrator(_contextMock.Object);

        result.Success.Should().BeFalse();
        result.CorrelationId.Should().Be(correlationId);
        result.ErrorMessage.Should().Be("Test error");
    }
}