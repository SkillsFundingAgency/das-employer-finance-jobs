using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Orchestrators;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.ImportPaymentsOrchestrator;

public class WhenProcessingPeriodEndActivity
{
    private Mock<ILogger<Orchestrators.ImportPaymentsOrchestrator>> _mockLogger;
    private Mock<IPeriodEndService> _mockPeriodEndService;
    private Orchestrators.ImportPaymentsOrchestrator _orchestrator;

    [SetUp]
    public void SetUp()
    {
        _mockLogger = new Mock<ILogger<Orchestrators.ImportPaymentsOrchestrator>>();
        _mockPeriodEndService = new Mock<IPeriodEndService>();
        _orchestrator = new Orchestrators.ImportPaymentsOrchestrator(_mockLogger.Object, _mockPeriodEndService.Object);
    }

    [Test]
    public async Task Then_Completes_Successfully()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        var input = new ProcessPeriodEndInput
        {
            CorrelationId = correlationId,
            PeriodEnd = new PeriodEnd
            {
                PeriodEndId = "PE-001",
                CalendarPeriodYear = 2024,
                CalendarPeriodMonth = 1,
                PaymentsForPeriod = "2024-01"
            }
        };

        SetupExpectedLogs(correlationId, 2024, "2024-01");

        // Act
        var act = async () => await _orchestrator.ProcessPeriodEndActivity(input);

        // Assert
        await act.Should().NotThrowAsync();
        _mockLogger.Verify();
    }

    [Test]
    public async Task Then_Logs_Processing_Information()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        var input = new ProcessPeriodEndInput
        {
            CorrelationId = correlationId,
            PeriodEnd = new PeriodEnd
            {
                PeriodEndId = "PE-001",
                CalendarPeriodYear = 2024,
                CalendarPeriodMonth = 1,
                PaymentsForPeriod = "2024-01"
            }
        };

        SetupExpectedLogs(correlationId, 2024, "2024-01");

        // Act
        await _orchestrator.ProcessPeriodEndActivity(input);

        // Assert
        _mockLogger.Verify();
    }   

    [Test]
    public async Task And_Different_Period_Values_Then_Logs_Correctly()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        var input = new ProcessPeriodEndInput
        {
            CorrelationId = correlationId,
            PeriodEnd = new PeriodEnd
            {
                PeriodEndId = "PE-002",
                CalendarPeriodYear = 2023,
                CalendarPeriodMonth = 12,
                PaymentsForPeriod = "2023-12"
            }
        };

        SetupExpectedLogs(correlationId, 2023, "2023-12");

        // Act
        await _orchestrator.ProcessPeriodEndActivity(input);

        // Assert
        _mockLogger.Verify();
    }

    private void SetupExpectedLogs(string correlationId, int year, string period)
    {
        _mockLogger.Setup(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Processing period end") && 
                                          v.ToString().Contains(correlationId) &&
                                          v.ToString().Contains(year.ToString()) &&
                                          v.ToString().Contains(period)),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()));
    }
}