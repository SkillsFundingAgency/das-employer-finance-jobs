using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces.Services;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.ImportPaymentsOrchestrator;

public class WhenGettingNewPeriodEndsActivity
{
    private Mock<ILogger<Jobs.ImportPaymentsOrchestrator>> _mockLogger;
    private Mock<IPeriodEndService> _mockPeriodEndService;
    private Jobs.ImportPaymentsOrchestrator _orchestrator;

    [SetUp]
    public void SetUp()
    {
        _mockLogger = new Mock<ILogger<Jobs.ImportPaymentsOrchestrator>>();
        _mockPeriodEndService = new Mock<IPeriodEndService>();
        _orchestrator = new Jobs.ImportPaymentsOrchestrator(_mockLogger.Object, _mockPeriodEndService.Object);
    }

    [Test]
    public async Task Then_Calls_PeriodEndService_With_CorrelationId()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        var expectedPeriodEnds = new List<PeriodEnd>
        {
            new PeriodEnd { PeriodEndId = "PE-001", CalendarPeriodYear = 2024, CalendarPeriodMonth = 1 }
        };

        _mockPeriodEndService
            .Setup(x => x.GetNewPeriodEndsAsync(correlationId))
            .ReturnsAsync(expectedPeriodEnds);

        // Act
        var result = await _orchestrator.GetNewPeriodEndsActivity(correlationId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedPeriodEnds);

        _mockPeriodEndService.Verify(x => x.GetNewPeriodEndsAsync(correlationId), Times.Once);
    }

    [Test]
    public async Task And_Service_Returns_Empty_List_Then_Returns_Empty_List()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
      
        _mockPeriodEndService.Setup(x => x.GetNewPeriodEndsAsync(correlationId)).ReturnsAsync(new List<PeriodEnd>());

        // Act
        var result = await _orchestrator.GetNewPeriodEndsActivity(correlationId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }
}
