using System.Linq;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Functions.Activities;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Orchestrators;
using SFA.DAS.Provider.Events.Api.Types;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Orchestrators.ImportPaymentsOrchestator;

[TestFixture]
public class WhenProcessingAccountOrchestratorRun
{
    private Mock<ILogger<ProcessAccountOrchestrator>> _loggerMock;
    private Mock<TaskOrchestrationContext> _contextMock;
    private ProcessAccountOrchestrator _orchestrator;

    [SetUp]
    public void SetUp()
    {
        _loggerMock = new Mock<ILogger<ProcessAccountOrchestrator>>();
        _contextMock = new Mock<TaskOrchestrationContext>();
        _orchestrator = new ProcessAccountOrchestrator(_loggerMock.Object);
        SetupRefreshPaymentDataCompletedEventPublished();
    }

    [Test]
    public async Task Then_Uses_The_CreateTransactionLines_Input_When_Payments_Are_Refreshed()
    {
        var payment = new Payment { Id = Guid.NewGuid().ToString() };
        var input = new ProcessAccountInput
        {
            AccountId = 12345,
            PeriodEndRef = "2024-01",
            CorrelationId = "correlation-id",
            IdempotencyKey = "idempotency-key"
        };

        _contextMock.Setup(context => context.GetInput<ProcessAccountInput>())
            .Returns(input);
        _contextMock.Setup(context => context.CallActivityAsync<AccountPaymentsImportResult>(
                It.IsAny<TaskName>(),
                It.IsAny<object>(),
                It.IsAny<TaskOptions>()))
            .ReturnsAsync(new AccountPaymentsImportResult
            {
                Payments = [payment],
                Status = "Succeeded",
                Message = "ok"
            });
        _contextMock.Setup(context => context.CallActivityAsync<AccountExistingPaymentIdsImportResult>(
                It.IsAny<TaskName>(),
                It.IsAny<object>(),
                It.IsAny<TaskOptions>()))
            .ReturnsAsync(new AccountExistingPaymentIdsImportResult
            {
                PaymentIds = [],
                Status = "Succeeded",
                Message = "ok"
            });
        _contextMock.Setup(context => context.CallActivityAsync<RefreshPaymentDataActivityResult>(
                It.IsAny<TaskName>(),
                It.IsAny<object>(),
                It.IsAny<TaskOptions>()))
            .ReturnsAsync(new RefreshPaymentDataActivityResult
            {
                PaymentsCreated = 1,
                PaymentDetails = [payment],
                Status = "Succeeded",
                Message = "ok"
            });
        _contextMock.Setup(context => context.CallActivityAsync<CreatePaymentTransactionLinesResult>(
                It.IsAny<TaskName>(),
                It.IsAny<object>(),
                It.IsAny<TaskOptions>()))
            .ReturnsAsync(new CreatePaymentTransactionLinesResult
            {
                TransactionsCreated = 1,
                Transactions = [],
                Status = "Succeeded",
                Message = "ok"
            });
        _contextMock.Setup(context => context.CallActivityAsync<CreatePaymentMetadataResult>(
                It.IsAny<TaskName>(),
                It.IsAny<object>(),
                It.IsAny<TaskOptions>()))
            .ReturnsAsync(new CreatePaymentMetadataResult
            {
                MetadataCreated = 1,
                Status = "Succeeded",
                Message = "ok"
            });

        var result = await _orchestrator.RunOrchestrator(_contextMock.Object);

        result.Success.Should().BeTrue();
        _contextMock.Verify(context => context.CallActivityAsync<RefreshPaymentDataActivityResult>(
                It.IsAny<TaskName>(),
                It.Is<RefreshPaymentDataInput>(refreshInput => refreshInput.AccountId == input.AccountId),
                It.IsAny<TaskOptions>()),
            Times.Once);
        _contextMock.Verify(context => context.CallActivityAsync<PublishRefreshPaymentDataCompletedEventResult>(
                It.Is<TaskName>(name => name.Name == nameof(RefreshPaymentDataCompletedEventActivities.PublishRefreshPaymentDataCompletedEventActivity)),
                It.Is<PublishRefreshPaymentDataCompletedEventInput>(publishInput =>
                    publishInput.AccountId == input.AccountId
                    && publishInput.PeriodEnd == input.PeriodEndRef
                    && publishInput.CorrelationId == input.CorrelationId
                    && publishInput.PaymentsProcessed),
                It.IsAny<TaskOptions>()),
            Times.Once);
        _contextMock.Verify(context => context.CallActivityAsync<CreatePaymentTransactionLinesResult>(
                It.IsAny<TaskName>(),
                It.Is<CreatePaymentTransactionLinesInput>(transactionInput =>
                    transactionInput.AccountId == input.AccountId
                    && transactionInput.PeriodEnd == input.PeriodEndRef
                    && transactionInput.CorrelationId == input.CorrelationId
                    && transactionInput.PaymentDetails.Count == 1
                    && transactionInput.PaymentDetails.Single().Id == payment.Id),
                It.IsAny<TaskOptions>()),
            Times.Once);
        _contextMock.Verify(context => context.CallActivityAsync<CreatePaymentMetadataResult>(
                It.IsAny<TaskName>(),
                It.Is<CreatePaymentMetadataInput>(metadataInput =>
                    metadataInput.AccountId == input.AccountId
                    && metadataInput.CorrelationId == input.CorrelationId
                    && metadataInput.PaymentDetails.Count == 1
                    && metadataInput.PaymentDetails.Single().Id == payment.Id),
                It.IsAny<TaskOptions>()),
            Times.Once);
    }

    [Test]
    public async Task Then_Does_Not_Start_Transaction_Line_Creation_When_No_New_Payments_Remain()
    {
        var input = new ProcessAccountInput
        {
            AccountId = 12345,
            PeriodEndRef = "2024-01",
            CorrelationId = "correlation-id",
            IdempotencyKey = "idempotency-key"
        };

        _contextMock.Setup(context => context.GetInput<ProcessAccountInput>())
            .Returns(input);
        _contextMock.Setup(context => context.CallActivityAsync<AccountPaymentsImportResult>(
                It.IsAny<TaskName>(),
                It.IsAny<object>(),
                It.IsAny<TaskOptions>()))
            .ReturnsAsync(new AccountPaymentsImportResult
            {
                Payments = [],
                Status = "Succeeded",
                Message = "ok"
            });
        _contextMock.Setup(context => context.CallActivityAsync<AccountExistingPaymentIdsImportResult>(
                It.IsAny<TaskName>(),
                It.IsAny<object>(),
                It.IsAny<TaskOptions>()))
            .ReturnsAsync(new AccountExistingPaymentIdsImportResult
            {
                PaymentIds = [],
                Status = "Succeeded",
                Message = "ok"
            });
        _contextMock.Setup(context => context.CallActivityAsync<RefreshPaymentDataActivityResult>(
                It.IsAny<TaskName>(),
                It.IsAny<object>(),
                It.IsAny<TaskOptions>()))
            .ReturnsAsync(new RefreshPaymentDataActivityResult
            {
                PaymentDetails = [],
                Status = "Succeeded",
                Message = "No new payments"
            });

        var result = await _orchestrator.RunOrchestrator(_contextMock.Object);

        result.Success.Should().BeTrue();
        _contextMock.Verify(context => context.CallActivityAsync<PublishRefreshPaymentDataCompletedEventResult>(
                It.Is<TaskName>(name => name.Name == nameof(RefreshPaymentDataCompletedEventActivities.PublishRefreshPaymentDataCompletedEventActivity)),
                It.Is<PublishRefreshPaymentDataCompletedEventInput>(publishInput =>
                    publishInput.AccountId == input.AccountId
                    && publishInput.PeriodEnd == input.PeriodEndRef
                    && publishInput.CorrelationId == input.CorrelationId
                    && !publishInput.PaymentsProcessed),
                It.IsAny<TaskOptions>()),
            Times.Once);
        _contextMock.Verify(context => context.CallActivityAsync<CreatePaymentTransactionLinesResult>(
                It.IsAny<TaskName>(),
                It.IsAny<object>(),
                It.IsAny<TaskOptions>()),
            Times.Never);
        _contextMock.Verify(context => context.CallActivityAsync<CreatePaymentMetadataResult>(
                It.IsAny<TaskName>(),
                It.IsAny<object>(),
                It.IsAny<TaskOptions>()),
            Times.Never);
    }

    [Test]
    public async Task Then_Starts_Transaction_Line_Creation_When_Metadata_Creation_Fails()
    {
        var payment = new Payment { Id = Guid.NewGuid().ToString() };
        var input = new ProcessAccountInput
        {
            AccountId = 12345,
            PeriodEndRef = "2024-01",
            CorrelationId = "correlation-id",
            IdempotencyKey = "idempotency-key"
        };

        SetupPaymentsStaged(input, payment);
        _contextMock.Setup(context => context.CallActivityAsync<CreatePaymentMetadataResult>(
                It.Is<TaskName>(name => name.Name == nameof(PaymentMetadataActivities.CreatePaymentMetadataActivity)),
                It.IsAny<object>(),
                It.IsAny<TaskOptions>()))
            .ThrowsAsync(new InvalidOperationException("Commitments API base URL is missing"));
        _contextMock.Setup(context => context.CallActivityAsync<CreatePaymentTransactionLinesResult>(
                It.Is<TaskName>(name => name.Name == nameof(PaymentTransactionLineActivities.CreatePaymentTransactionLinesActivity)),
                It.IsAny<object>(),
                It.IsAny<TaskOptions>()))
            .ReturnsAsync(new CreatePaymentTransactionLinesResult
            {
                TransactionsCreated = 1,
                Transactions = [],
                Status = "Succeeded",
                Message = "ok"
            });

        var result = await _orchestrator.RunOrchestrator(_contextMock.Object);

        result.Success.Should().BeFalse();
        _contextMock.Verify(context => context.CallActivityAsync<CreatePaymentTransactionLinesResult>(
                It.Is<TaskName>(name => name.Name == nameof(PaymentTransactionLineActivities.CreatePaymentTransactionLinesActivity)),
                It.Is<CreatePaymentTransactionLinesInput>(transactionInput =>
                    transactionInput.AccountId == input.AccountId
                    && transactionInput.PeriodEnd == input.PeriodEndRef
                    && transactionInput.CorrelationId == input.CorrelationId
                    && transactionInput.PaymentDetails.Count == 1
                    && transactionInput.PaymentDetails.Single().Id == payment.Id),
                It.IsAny<TaskOptions>()),
            Times.Once);
    }

    [Test]
    public async Task Then_Continues_Account_Processing_When_Refresh_Payment_Data_Completed_Event_Publishing_Fails()
    {
        var payment = new Payment { Id = Guid.NewGuid().ToString() };
        var input = new ProcessAccountInput
        {
            AccountId = 12345,
            PeriodEndRef = "2024-01",
            CorrelationId = "correlation-id",
            IdempotencyKey = "idempotency-key"
        };

        SetupPaymentsStaged(input, payment);
        _contextMock.Setup(context => context.CallActivityAsync<PublishRefreshPaymentDataCompletedEventResult>(
                It.Is<TaskName>(name => name.Name == nameof(RefreshPaymentDataCompletedEventActivities.PublishRefreshPaymentDataCompletedEventActivity)),
                It.IsAny<object>(),
                It.IsAny<TaskOptions>()))
            .ThrowsAsync(new InvalidOperationException("Service Bus publish failed"));
        _contextMock.Setup(context => context.CallActivityAsync<CreatePaymentMetadataResult>(
                It.Is<TaskName>(name => name.Name == nameof(PaymentMetadataActivities.CreatePaymentMetadataActivity)),
                It.IsAny<object>(),
                It.IsAny<TaskOptions>()))
            .ReturnsAsync(new CreatePaymentMetadataResult
            {
                MetadataCreated = 1,
                Status = "Succeeded",
                Message = "ok"
            });
        _contextMock.Setup(context => context.CallActivityAsync<CreatePaymentTransactionLinesResult>(
                It.Is<TaskName>(name => name.Name == nameof(PaymentTransactionLineActivities.CreatePaymentTransactionLinesActivity)),
                It.IsAny<object>(),
                It.IsAny<TaskOptions>()))
            .ReturnsAsync(new CreatePaymentTransactionLinesResult
            {
                TransactionsCreated = 1,
                Transactions = [],
                Status = "Succeeded",
                Message = "ok"
            });

        var result = await _orchestrator.RunOrchestrator(_contextMock.Object);

        result.Success.Should().BeTrue();
        _contextMock.Verify(context => context.CallActivityAsync<CreatePaymentTransactionLinesResult>(
                It.Is<TaskName>(name => name.Name == nameof(PaymentTransactionLineActivities.CreatePaymentTransactionLinesActivity)),
                It.IsAny<object>(),
                It.IsAny<TaskOptions>()),
            Times.Once);
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("PublishRefreshPaymentDataCompletedEventActivity failed")),
                It.Is<InvalidOperationException>(exception => exception.Message == "Service Bus publish failed"),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Test]
    public async Task Then_Returns_Failed_Result_When_Transaction_Line_Creation_Fails()
    {
        var payment = new Payment { Id = Guid.NewGuid().ToString() };
        var input = new ProcessAccountInput
        {
            AccountId = 12345,
            PeriodEndRef = "2024-01",
            CorrelationId = "correlation-id",
            IdempotencyKey = "idempotency-key"
        };

        SetupPaymentsStaged(input, payment);
        _contextMock.Setup(context => context.CallActivityAsync<CreatePaymentMetadataResult>(
                It.Is<TaskName>(name => name.Name == nameof(PaymentMetadataActivities.CreatePaymentMetadataActivity)),
                It.IsAny<object>(),
                It.IsAny<TaskOptions>()))
            .ReturnsAsync(new CreatePaymentMetadataResult
            {
                MetadataCreated = 1,
                Status = "Succeeded",
                Message = "ok"
            });
        _contextMock.Setup(context => context.CallActivityAsync<CreatePaymentTransactionLinesResult>(
                It.Is<TaskName>(name => name.Name == nameof(PaymentTransactionLineActivities.CreatePaymentTransactionLinesActivity)),
                It.IsAny<object>(),
                It.IsAny<TaskOptions>()))
            .ThrowsAsync(new InvalidOperationException("Finance API returned BadRequest"));

        var result = await _orchestrator.RunOrchestrator(_contextMock.Object);

        result.Success.Should().BeFalse();
        _contextMock.Verify(context => context.CallActivityAsync<CreatePaymentTransactionLinesResult>(
                It.Is<TaskName>(name => name.Name == nameof(PaymentTransactionLineActivities.CreatePaymentTransactionLinesActivity)),
                It.IsAny<object>(),
                It.IsAny<TaskOptions>()),
            Times.Once);
    }

    [Test]
    public async Task Then_Does_Not_Start_Transaction_Line_Creation_When_Refresh_Payment_Data_Fails()
    {
        var payment = new Payment { Id = Guid.NewGuid().ToString() };
        var input = new ProcessAccountInput
        {
            AccountId = 12345,
            PeriodEndRef = "2024-01",
            CorrelationId = "correlation-id",
            IdempotencyKey = "idempotency-key"
        };

        _contextMock.Setup(context => context.GetInput<ProcessAccountInput>())
            .Returns(input);
        _contextMock.Setup(context => context.CallActivityAsync<AccountPaymentsImportResult>(
                It.IsAny<TaskName>(),
                It.IsAny<object>(),
                It.IsAny<TaskOptions>()))
            .ReturnsAsync(new AccountPaymentsImportResult
            {
                Payments = [payment],
                Status = "Succeeded",
                Message = "ok"
            });
        _contextMock.Setup(context => context.CallActivityAsync<AccountExistingPaymentIdsImportResult>(
                It.IsAny<TaskName>(),
                It.IsAny<object>(),
                It.IsAny<TaskOptions>()))
            .ReturnsAsync(new AccountExistingPaymentIdsImportResult
            {
                PaymentIds = [],
                Status = "Succeeded",
                Message = "ok"
            });
        _contextMock.Setup(context => context.CallActivityAsync<RefreshPaymentDataActivityResult>(
                It.IsAny<TaskName>(),
                It.IsAny<object>(),
                It.IsAny<TaskOptions>()))
            .ReturnsAsync(new RefreshPaymentDataActivityResult
            {
                PaymentDetails = [payment],
                Status = "Failed",
                Message = "Finance API returned BadRequest"
            });

        var result = await _orchestrator.RunOrchestrator(_contextMock.Object);

        result.Success.Should().BeFalse();
        _contextMock.Verify(context => context.CallActivityAsync<PublishRefreshPaymentDataCompletedEventResult>(
                It.IsAny<TaskName>(),
                It.IsAny<object>(),
                It.IsAny<TaskOptions>()),
            Times.Never);
        _contextMock.Verify(context => context.CallActivityAsync<CreatePaymentTransactionLinesResult>(
                It.IsAny<TaskName>(),
                It.IsAny<object>(),
                It.IsAny<TaskOptions>()),
            Times.Never);
        _contextMock.Verify(context => context.CallActivityAsync<CreatePaymentMetadataResult>(
                It.IsAny<TaskName>(),
                It.IsAny<object>(),
                It.IsAny<TaskOptions>()),
            Times.Never);
    }

    private void SetupPaymentsStaged(ProcessAccountInput input, Payment payment)
    {
        _contextMock.Setup(context => context.GetInput<ProcessAccountInput>())
            .Returns(input);
        _contextMock.Setup(context => context.CallActivityAsync<AccountPaymentsImportResult>(
                It.IsAny<TaskName>(),
                It.IsAny<object>(),
                It.IsAny<TaskOptions>()))
            .ReturnsAsync(new AccountPaymentsImportResult
            {
                Payments = [payment],
                Status = "Succeeded",
                Message = "ok"
            });
        _contextMock.Setup(context => context.CallActivityAsync<AccountExistingPaymentIdsImportResult>(
                It.IsAny<TaskName>(),
                It.IsAny<object>(),
                It.IsAny<TaskOptions>()))
            .ReturnsAsync(new AccountExistingPaymentIdsImportResult
            {
                PaymentIds = [],
                Status = "Succeeded",
                Message = "ok"
            });
        _contextMock.Setup(context => context.CallActivityAsync<RefreshPaymentDataActivityResult>(
                It.IsAny<TaskName>(),
                It.IsAny<object>(),
                It.IsAny<TaskOptions>()))
            .ReturnsAsync(new RefreshPaymentDataActivityResult
            {
                PaymentsCreated = 1,
                PaymentDetails = [payment],
                Status = "Succeeded",
                Message = "ok"
            });
    }

    private void SetupRefreshPaymentDataCompletedEventPublished()
    {
        _contextMock.Setup(context => context.CallActivityAsync<PublishRefreshPaymentDataCompletedEventResult>(
                It.Is<TaskName>(name => name.Name == nameof(RefreshPaymentDataCompletedEventActivities.PublishRefreshPaymentDataCompletedEventActivity)),
                It.IsAny<object>(),
                It.IsAny<TaskOptions>()))
            .ReturnsAsync(new PublishRefreshPaymentDataCompletedEventResult
            {
                Status = "Succeeded",
                Message = "ok"
            });
    }
}
