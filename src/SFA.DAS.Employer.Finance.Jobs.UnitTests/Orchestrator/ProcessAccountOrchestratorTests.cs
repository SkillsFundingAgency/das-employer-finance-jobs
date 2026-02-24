using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using SFA.DAS.Employer.Finance.Jobs.Functions.Orchestrators;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models.PaymentTransactions;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models.RefreshPayments;
using SFA.DAS.Provider.Events.Api.Types;


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
            CorrelationId = "corr-1234",
            PeriodEndRef = "2425-R01",
            IdempotencyKey = "idem-key"
        };

        var mockContext = new Mock<TaskOrchestrationContext>();
        mockContext
            .Setup(c => c.GetInput<ProcessAccountInput>())
            .Returns(input);

        var refreshResult = new RefreshPaymentDataResult
        {
            PaymentsCreated = 0,
            PaymentDetails = Array.Empty<Payment>()
        };
        mockContext
            .Setup(c => c.CallActivityAsync<RefreshPaymentDataResult>(It.IsAny<TaskName>(), It.IsAny<object>(), null))
            .ReturnsAsync(refreshResult);

        var createTransactionLinesResult = new CreatePaymentTransactionLinesResult { TransactionsCreated = 0 };
        mockContext
            .Setup(c => c.CallActivityAsync<CreatePaymentTransactionLinesResult>(It.IsAny<TaskName>(), It.IsAny<object>(), null))
            .ReturnsAsync(createTransactionLinesResult);

        var mockRefreshPaymentDataService = new Mock<IRefreshPaymentDataService>();
        var mockPaymentTransactionLineService = new Mock<IPaymentTransactionLinesService>();
        var orchestrator = new ProcessAccountOrchestrator(
            NullLogger<ProcessAccountOrchestrator>.Instance,
            mockRefreshPaymentDataService.Object,
            mockPaymentTransactionLineService.Object);

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
    public void RunOrchestrator_WithNullInput_ThrowsNullReferenceException()
    {
        // Arrange
        var mockContext = new Mock<TaskOrchestrationContext>();
        mockContext
            .Setup(c => c.GetInput<ProcessAccountInput>())
            .Returns((ProcessAccountInput?)null);

        var mockRefreshPaymentDataService = new Mock<IRefreshPaymentDataService>();
        var mockPaymentTransactionLineService = new Mock<IPaymentTransactionLinesService>();
        var orchestrator = new ProcessAccountOrchestrator(
            NullLogger<ProcessAccountOrchestrator>.Instance,
            mockRefreshPaymentDataService.Object,
            mockPaymentTransactionLineService.Object);

        // Act & Assert - orchestrator dereferences input when building activity input
        var act = async () => await orchestrator.RunOrchestrator(mockContext.Object);
        act.Should().ThrowAsync<NullReferenceException>();
    }
}