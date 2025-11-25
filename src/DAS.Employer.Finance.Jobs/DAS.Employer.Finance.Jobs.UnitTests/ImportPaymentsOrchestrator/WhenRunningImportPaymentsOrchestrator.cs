using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces.Services;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.ImportPaymentsOrchestrator;

public class WhenRunningImportPaymentsOrchestrator
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
    public void Then_Can_Be_Instantiated()
    {
        // Act & Assert
        _orchestrator.Should().NotBeNull();
    }

}
