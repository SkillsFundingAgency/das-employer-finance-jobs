using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Orchestrators.ImportPaymentsOrchestrator;

public class WhenRunningImportPaymentsOrchestrator
{
    private Mock<ILogger<Orchestrators.ImportPaymentsOrchestrator>> _mockLogger;
    private Mock<IPeriodEndService> _mockPeriodEndService;
    private Orchestrators.ImportPaymentsOrchestrator _orchestrator;

    [SetUp]
    public void SetUp()
    {
        _mockLogger = new Mock<ILogger<ImportPaymentsOrchestrator>>();
        _mockPeriodEndService = new Mock<IPeriodEndService>();
        _orchestrator = new ImportPaymentsOrchestrator(_mockLogger.Object, _mockPeriodEndService.Object);
    }

    [Test]
    public void Then_Can_Be_Instantiated()
    {
        // Act & Assert
        _orchestrator.Should().NotBeNull();
    }
}