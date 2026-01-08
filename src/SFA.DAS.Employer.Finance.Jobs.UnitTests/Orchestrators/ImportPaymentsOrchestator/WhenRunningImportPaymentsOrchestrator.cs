using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Orchestrators;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Orchestrators.ImportPaymentsOrchestator;

public class WhenRunningImportPaymentsOrchestrator
{
    private Mock<ILogger<ImportPaymentsOrchestrator>> _mockLogger;
    private Mock<IPeriodEndService> _mockPeriodEndService;
    private ImportPaymentsOrchestrator _orchestrator;

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