using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using NServiceBus;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Orchestrators;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Orchestrators.ProcessPeriodEndOrchestratorTests;

[TestFixture]
public class WhenRunningProcessPeriodEndOrchestrator
{
    private Mock<ILogger<ProcessPeriodEndOrchestrator>> _loggerMock;
    private Mock<IPeriodEndService> _periodEndServiceMock;
    private Mock<IAccountService> _accountServiceMock;
    private Mock<IFunctionEndpoint> _functionEndpointMock;
    private Mock<TaskOrchestrationContext> _contextMock;

    private ProcessPeriodEndOrchestrator _orchestrator;

    [SetUp]
    public void SetUp()
    {
        _loggerMock = new Mock<ILogger<ProcessPeriodEndOrchestrator>>();
        _periodEndServiceMock = new Mock<IPeriodEndService>();
        _accountServiceMock = new Mock<IAccountService>();
        _functionEndpointMock = new Mock<IFunctionEndpoint>();
        _contextMock = new Mock<TaskOrchestrationContext>();

        _contextMock
            .Setup(c => c.CurrentUtcDateTime)
            .Returns(DateTime.UtcNow);

        _orchestrator = new ProcessPeriodEndOrchestrator(
            _loggerMock.Object,
            _periodEndServiceMock.Object,
            _accountServiceMock.Object,
            _functionEndpointMock.Object);
    }

    [Test]
    public void Then_Can_Be_Instantiated()
    {
        // Assert
        _orchestrator.Should().NotBeNull();
    }

    [Test]
    public async Task Then_Returns_Result_When_Activity_Publishes_Commands()
    {
        // Arrange
        var input = CreateValidPeriodEnd("PE-202401");
        var correlationId = Guid.NewGuid();
        var createdPeriodEnd = new PeriodEnd { Id = 123, PeriodEndId = "PE-202401" };

        _contextMock.Setup(c => c.GetInput<PeriodEnd>())
            .Returns(input);

        _contextMock.Setup(c => c.NewGuid())
            .Returns(correlationId);

        _contextMock
            .Setup(c => c.CallActivityAsync<PeriodEnd>(
                It.Is<TaskName>(t => t.Name == nameof(ProcessPeriodEndOrchestrator.CreatePeriodEndActivity)),
                It.Is<CreatePeriodEndActivityInput>(i => i.PeriodEnd == input && i.CorrelationId == correlationId),
                It.IsAny<TaskOptions>()))
            .ReturnsAsync(createdPeriodEnd);

        _contextMock
            .Setup(c => c.CallActivityAsync<int>(
                It.Is<TaskName>(t => t.Name == nameof(ProcessPeriodEndOrchestrator.PublishAccountPaymentCommandsActivity)),
                It.Is<PublishAccountPaymentCommandsInput>(i => i.PeriodEndRef == "PE-202401" && i.PeriodEnd == createdPeriodEnd),
                It.IsAny<TaskOptions>()))
            .ReturnsAsync(2);

        // Act
        var result = await _orchestrator.Run(_contextMock.Object);

        // Assert
        result.Should().NotBeNull();
        result.PeriodEndId.Should().Be("123");
        result.TotalCommandsPublished.Should().Be(2);

        _contextMock.VerifyAll();
        _contextMock.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Then_Returns_Total_Commands_Published_From_Activity()
    {
        // Arrange
        var input = CreateValidPeriodEnd("PE-PAGED");
        var correlationId = Guid.NewGuid();
        var createdPeriodEnd = new PeriodEnd { Id = 456, PeriodEndId = "PE-PAGED" };

        _contextMock.Setup(c => c.GetInput<PeriodEnd>())
            .Returns(input);

        _contextMock.Setup(c => c.NewGuid())
            .Returns(correlationId);

        _contextMock
            .Setup(c => c.CallActivityAsync<PeriodEnd>(
                It.Is<TaskName>(t => t.Name == nameof(ProcessPeriodEndOrchestrator.CreatePeriodEndActivity)),
                It.Is<CreatePeriodEndActivityInput>(i => i.PeriodEnd == input && i.CorrelationId == correlationId),
                It.IsAny<TaskOptions>()))
            .ReturnsAsync(createdPeriodEnd);

        _contextMock
            .Setup(c => c.CallActivityAsync<int>(
                It.Is<TaskName>(t => t.Name == nameof(ProcessPeriodEndOrchestrator.PublishAccountPaymentCommandsActivity)),
                It.Is<PublishAccountPaymentCommandsInput>(i => i.PeriodEndRef == "PE-PAGED" && i.PeriodEnd == createdPeriodEnd),
                It.IsAny<TaskOptions>()))
            .ReturnsAsync(10005);

        // Act
        var result = await _orchestrator.Run(_contextMock.Object);

        // Assert
        result.PeriodEndId.Should().Be("456");
        result.TotalCommandsPublished.Should().Be(10005);

        _contextMock.VerifyAll();
        _contextMock.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Then_Returns_Zero_When_Activity_Publishes_No_Commands()
    {
        // Arrange
        var input = CreateValidPeriodEnd("PE-EMPTY");
        var correlationId = Guid.NewGuid();
        var createdPeriodEnd = new PeriodEnd { Id = 999, PeriodEndId = "PE-EMPTY" };

        _contextMock.Setup(c => c.GetInput<PeriodEnd>())
            .Returns(input);

        _contextMock.Setup(c => c.NewGuid())
            .Returns(correlationId);

        _contextMock
            .Setup(c => c.CallActivityAsync<PeriodEnd>(
                It.Is<TaskName>(t => t.Name == nameof(ProcessPeriodEndOrchestrator.CreatePeriodEndActivity)),
                It.Is<CreatePeriodEndActivityInput>(i => i.PeriodEnd == input && i.CorrelationId == correlationId),
                It.IsAny<TaskOptions>()))
            .ReturnsAsync(createdPeriodEnd);

        _contextMock
            .Setup(c => c.CallActivityAsync<int>(
                It.Is<TaskName>(t => t.Name == nameof(ProcessPeriodEndOrchestrator.PublishAccountPaymentCommandsActivity)),
                It.Is<PublishAccountPaymentCommandsInput>(i => i.PeriodEndRef == "PE-EMPTY" && i.PeriodEnd == createdPeriodEnd),
                It.IsAny<TaskOptions>()))
            .ReturnsAsync(0);

        // Act
        var result = await _orchestrator.Run(_contextMock.Object);

        // Assert
        result.TotalCommandsPublished.Should().Be(0);

        _contextMock.VerifyAll();
        _contextMock.VerifyNoOtherCalls();
    }

    [Test]
    public void Then_Throws_When_AccountDataValidAt_Is_Missing()
    {
        // Arrange
        var input = new PeriodEnd
        {
            PeriodEndId = "PE-INVALID",
            CommitmentDataValidAt = DateTime.UtcNow.AddMinutes(-1)
        };

        _contextMock.Setup(c => c.GetInput<PeriodEnd>())
            .Returns(input);

        // Act
        Func<Task> act = () => _orchestrator.Run(_contextMock.Object);

        // Assert
        act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("AccountDataValidAt must be provided.");
    }

    [Test]
    public void Then_Throws_When_CommitmentDataValidAt_Is_Missing()
    {
        // Arrange
        var input = new PeriodEnd
        {
            PeriodEndId = "PE-INVALID",
            AccountDataValidAt = DateTime.UtcNow.AddMinutes(-1)
        };

        _contextMock.Setup(c => c.GetInput<PeriodEnd>())
            .Returns(input);

        // Act
        Func<Task> act = () => _orchestrator.Run(_contextMock.Object);

        // Assert
        act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("CommitmentDataValidAt must be provided.");
    }

    [Test]
    public void Then_Throws_When_Dates_Are_In_The_Future()
    {
        // Arrange
        var input = new PeriodEnd
        {
            PeriodEndId = "PE-FUTURE",
            AccountDataValidAt = DateTime.UtcNow.AddMinutes(10),
            CommitmentDataValidAt = DateTime.UtcNow.AddMinutes(10)
        };

        _contextMock.Setup(c => c.GetInput<PeriodEnd>())
            .Returns(input);

        // Act
        Func<Task> act = () => _orchestrator.Run(_contextMock.Object);

        // Assert
        act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Test]
    public void Then_Throws_When_Input_Is_Null()
    {
        // Arrange
        _contextMock.Setup(c => c.GetInput<PeriodEnd>())
            .Returns((PeriodEnd)null);

        // Act
        Func<Task> act = () => _orchestrator.Run(_contextMock.Object);

        // Assert
        act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("input");
    }

    [Test]
    public async Task And_PeriodEndId_Is_Null_Then_Uses_Id_For_PeriodEndRef()
    {
        // Arrange
        var input = CreateValidPeriodEnd("PE-NULLREF");
        var correlationId = Guid.NewGuid();
        var createdPeriodEnd = new PeriodEnd { Id = 777, PeriodEndId = null };

        _contextMock.Setup(c => c.GetInput<PeriodEnd>())
            .Returns(input);

        _contextMock.Setup(c => c.NewGuid())
            .Returns(correlationId);

        _contextMock
            .Setup(c => c.CallActivityAsync<PeriodEnd>(
                It.Is<TaskName>(t => t.Name == nameof(ProcessPeriodEndOrchestrator.CreatePeriodEndActivity)),
                It.Is<CreatePeriodEndActivityInput>(i => i.PeriodEnd == input && i.CorrelationId == correlationId),
                It.IsAny<TaskOptions>()))
            .ReturnsAsync(createdPeriodEnd);

        _contextMock
            .Setup(c => c.CallActivityAsync<int>(
                It.Is<TaskName>(t => t.Name == nameof(ProcessPeriodEndOrchestrator.PublishAccountPaymentCommandsActivity)),
                It.Is<PublishAccountPaymentCommandsInput>(i => i.PeriodEndRef == "777" && i.PeriodEnd == createdPeriodEnd),
                It.IsAny<TaskOptions>()))
            .ReturnsAsync(0);

        // Act
        var result = await _orchestrator.Run(_contextMock.Object);

        // Assert
        result.Should().NotBeNull();
        result.PeriodEndId.Should().Be("777");

        _contextMock.VerifyAll();
        _contextMock.VerifyNoOtherCalls();
    }

    private static PeriodEnd CreateValidPeriodEnd(string periodEndId)
    {
        return new PeriodEnd
        {
            PeriodEndId = periodEndId,
            AccountDataValidAt = DateTime.UtcNow.AddMinutes(-1),
            CommitmentDataValidAt = DateTime.UtcNow.AddMinutes(-1)
        };
    }
}
