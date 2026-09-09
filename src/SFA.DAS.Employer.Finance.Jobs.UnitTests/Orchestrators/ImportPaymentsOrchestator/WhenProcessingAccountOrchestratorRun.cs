using System.Linq;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Functions.Activities;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Orchestrators;

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
        SetupTransferStagedToOperationalSkipped();
    }

    [Test]
    public async Task Then_Pages_Staging_And_Passes_Slim_Transfer_Lookups_Without_Payment_Lists()
    {
        var paymentId = Guid.NewGuid();
        var lookup = new TransferPaymentLookup { PaymentId = paymentId, Ukprn = 100 };
        var input = new ProcessAccountInput
        {
            AccountId = 12345,
            AccountName = "Receiver Account",
            PeriodEndRef = "2024-01",
            CorrelationId = "correlation-id",
            IdempotencyKey = "idempotency-key",
            TriggeredAt = new DateTime(2025, 11, 18, 10, 0, 0, DateTimeKind.Utc)
        };

        _contextMock.Setup(context => context.GetInput<ProcessAccountInput>())
            .Returns(input);
        _contextMock.Setup(context => context.CallActivityAsync<StageAccountPaymentsPageResult>(
                It.IsAny<TaskName>(),
                It.IsAny<object>(),
                It.IsAny<TaskOptions>()))
            .ReturnsAsync(new StageAccountPaymentsPageResult
            {
                PageNumber = 1,
                TotalPages = 1,
                ItemCount = 1,
                PaymentsCreated = 1,
                MetadataCreated = 1,
                TransactionsCreated = 1,
                TransferLookups = [lookup],
                Status = "Succeeded",
                Message = "ok"
            });
        _contextMock.Setup(context => context.CallActivityAsync<RefreshAccountTransfersResult>(
                It.IsAny<TaskName>(),
                It.IsAny<object>(),
                It.IsAny<TaskOptions>()))
            .ReturnsAsync(new RefreshAccountTransfersResult
            {
                TransfersProcessed = 2,
                Status = "Succeeded",
                Message = "ok"
            });

        var result = await _orchestrator.RunOrchestrator(_contextMock.Object);

        result.Success.Should().BeTrue();
        result.PaymentsProcessed.Should().Be(1);
        result.TransfersProcessed.Should().Be(2);

        _contextMock.Verify(context => context.CallActivityAsync<StageAccountPaymentsPageResult>(
                It.Is<TaskName>(name => name.Name == nameof(AccountPaymentPageStagingActivities.StageAccountPaymentsPageActivity)),
                It.Is<StageAccountPaymentsPageInput>(pageInput =>
                    pageInput.AccountId == input.AccountId
                    && pageInput.PeriodEndRef == input.PeriodEndRef
                    && pageInput.PageNumber == 1
                    && pageInput.CorrelationId == input.CorrelationId),
                It.IsAny<TaskOptions>()),
            Times.Once);

        _contextMock.Verify(context => context.CallActivityAsync<AccountPaymentsImportResult>(
                It.IsAny<TaskName>(),
                It.IsAny<object>(),
                It.IsAny<TaskOptions>()),
            Times.Never);
        _contextMock.Verify(context => context.CallActivityAsync<AccountExistingPaymentIdsImportResult>(
                It.IsAny<TaskName>(),
                It.IsAny<object>(),
                It.IsAny<TaskOptions>()),
            Times.Never);
        _contextMock.Verify(context => context.CallActivityAsync<RefreshPaymentDataActivityResult>(
                It.IsAny<TaskName>(),
                It.IsAny<object>(),
                It.IsAny<TaskOptions>()),
            Times.Never);
        _contextMock.Verify(context => context.CallActivityAsync<CreatePaymentMetadataResult>(
                It.IsAny<TaskName>(),
                It.IsAny<object>(),
                It.IsAny<TaskOptions>()),
            Times.Never);
        _contextMock.Verify(context => context.CallActivityAsync<CreatePaymentTransactionLinesResult>(
                It.IsAny<TaskName>(),
                It.IsAny<object>(),
                It.IsAny<TaskOptions>()),
            Times.Never);

        _contextMock.Verify(context => context.CallActivityAsync<PublishRefreshPaymentDataCompletedEventResult>(
                It.Is<TaskName>(name => name.Name == nameof(RefreshPaymentDataCompletedEventActivities.PublishRefreshPaymentDataCompletedEventActivity)),
                It.Is<PublishRefreshPaymentDataCompletedEventInput>(publishInput =>
                    publishInput.AccountId == input.AccountId
                    && publishInput.PeriodEnd == input.PeriodEndRef
                    && publishInput.CorrelationId == input.CorrelationId
                    && publishInput.PaymentsProcessed),
                It.IsAny<TaskOptions>()),
            Times.Once);
        _contextMock.Verify(context => context.CallActivityAsync<RefreshAccountTransfersResult>(
                It.Is<TaskName>(name => name.Name == nameof(AccountTransferActivities.RefreshAccountTransfersActivity)),
                It.Is<RefreshAccountTransfersInput>(transferInput =>
                    transferInput.AccountId == input.AccountId
                    && transferInput.Payments.Count == 1
                    && transferInput.Payments.Single().PaymentId == paymentId),
                It.IsAny<TaskOptions>()),
            Times.Once);
        _contextMock.Verify(context => context.CallActivityAsync<TransferStagedToOperationalResult>(
                It.IsAny<TaskName>(),
                It.IsAny<object>(),
                It.IsAny<TaskOptions>()),
            Times.Once);
    }

    [Test]
    public async Task Then_Takes_Empty_Account_Fast_Path_When_First_Page_Has_No_Payments()
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
        _contextMock.Setup(context => context.CallActivityAsync<StageAccountPaymentsPageResult>(
                It.IsAny<TaskName>(),
                It.IsAny<object>(),
                It.IsAny<TaskOptions>()))
            .ReturnsAsync(new StageAccountPaymentsPageResult
            {
                PageNumber = 1,
                TotalPages = 0,
                ItemCount = 0,
                TransferLookups = [],
                Status = "Succeeded",
                Message = "No payments on page."
            });
        _contextMock.Setup(context => context.CallActivityAsync<RefreshAccountTransfersResult>(
                It.IsAny<TaskName>(),
                It.IsAny<object>(),
                It.IsAny<TaskOptions>()))
            .ReturnsAsync(new RefreshAccountTransfersResult
            {
                TransfersProcessed = 0,
                Status = "Succeeded",
                Message = "No transfers"
            });

        var result = await _orchestrator.RunOrchestrator(_contextMock.Object);

        result.Success.Should().BeTrue();
        result.PaymentsProcessed.Should().Be(0);
        _contextMock.Verify(context => context.CallActivityAsync<StageAccountPaymentsPageResult>(
                It.IsAny<TaskName>(),
                It.IsAny<object>(),
                It.IsAny<TaskOptions>()),
            Times.Once);
        _contextMock.Verify(context => context.CallActivityAsync<PublishRefreshPaymentDataCompletedEventResult>(
                It.IsAny<TaskName>(),
                It.IsAny<object>(),
                It.IsAny<TaskOptions>()),
            Times.Never);
        _contextMock.Verify(context => context.CallActivityAsync<TransferStagedToOperationalResult>(
                It.IsAny<TaskName>(),
                It.IsAny<object>(),
                It.IsAny<TaskOptions>()),
            Times.Never);
        _contextMock.Verify(context => context.CallActivityAsync<RefreshAccountTransfersResult>(
                It.IsAny<TaskName>(),
                It.IsAny<object>(),
                It.IsAny<TaskOptions>()),
            Times.Once);
    }

    [Test]
    public async Task Then_Loops_Provider_Events_Pages_Without_Exceeding_Page_Bound_Per_Result()
    {
        var input = new ProcessAccountInput
        {
            AccountId = 12345,
            PeriodEndRef = "2024-01",
            CorrelationId = "correlation-id",
            IdempotencyKey = "idempotency-key"
        };

        var pageOneLookups = Enumerable.Range(0, StageAccountPaymentsPageResult.MaxTransferLookupsPerPage)
            .Select(_ => new TransferPaymentLookup { PaymentId = Guid.NewGuid() })
            .ToList();
        var pageTwoLookups = Enumerable.Range(0, 500)
            .Select(_ => new TransferPaymentLookup { PaymentId = Guid.NewGuid() })
            .ToList();

        _contextMock.Setup(context => context.GetInput<ProcessAccountInput>())
            .Returns(input);
        _contextMock.Setup(context => context.CallActivityAsync<StageAccountPaymentsPageResult>(
                It.Is<TaskName>(name => name.Name == nameof(AccountPaymentPageStagingActivities.StageAccountPaymentsPageActivity)),
                It.Is<StageAccountPaymentsPageInput>(pageInput => pageInput.PageNumber == 1),
                It.IsAny<TaskOptions>()))
            .ReturnsAsync(new StageAccountPaymentsPageResult
            {
                PageNumber = 1,
                TotalPages = 3,
                ItemCount = pageOneLookups.Count,
                PaymentsCreated = pageOneLookups.Count,
                TransferLookups = pageOneLookups,
                Status = "Succeeded",
                Message = "ok"
            });
        _contextMock.Setup(context => context.CallActivityAsync<StageAccountPaymentsPageResult>(
                It.Is<TaskName>(name => name.Name == nameof(AccountPaymentPageStagingActivities.StageAccountPaymentsPageActivity)),
                It.Is<StageAccountPaymentsPageInput>(pageInput => pageInput.PageNumber == 2),
                It.IsAny<TaskOptions>()))
            .ReturnsAsync(new StageAccountPaymentsPageResult
            {
                PageNumber = 2,
                TotalPages = 3,
                ItemCount = pageOneLookups.Count,
                PaymentsCreated = pageOneLookups.Count,
                TransferLookups = pageOneLookups,
                Status = "Succeeded",
                Message = "ok"
            });
        _contextMock.Setup(context => context.CallActivityAsync<StageAccountPaymentsPageResult>(
                It.Is<TaskName>(name => name.Name == nameof(AccountPaymentPageStagingActivities.StageAccountPaymentsPageActivity)),
                It.Is<StageAccountPaymentsPageInput>(pageInput => pageInput.PageNumber == 3),
                It.IsAny<TaskOptions>()))
            .ReturnsAsync(new StageAccountPaymentsPageResult
            {
                PageNumber = 3,
                TotalPages = 3,
                ItemCount = pageTwoLookups.Count,
                PaymentsCreated = pageTwoLookups.Count,
                TransferLookups = pageTwoLookups,
                Status = "Succeeded",
                Message = "ok"
            });
        _contextMock.Setup(context => context.CallActivityAsync<RefreshAccountTransfersResult>(
                It.IsAny<TaskName>(),
                It.IsAny<object>(),
                It.IsAny<TaskOptions>()))
            .ReturnsAsync(new RefreshAccountTransfersResult
            {
                TransfersProcessed = 0,
                Status = "Succeeded",
                Message = "ok"
            });

        var result = await _orchestrator.RunOrchestrator(_contextMock.Object);

        result.Success.Should().BeTrue();
        result.PaymentsProcessed.Should().Be(pageOneLookups.Count * 2 + pageTwoLookups.Count);

        _contextMock.Verify(context => context.CallActivityAsync<StageAccountPaymentsPageResult>(
                It.IsAny<TaskName>(),
                It.IsAny<object>(),
                It.IsAny<TaskOptions>()),
            Times.Exactly(3));
        _contextMock.Verify(context => context.CallActivityAsync<RefreshAccountTransfersResult>(
                It.Is<TaskName>(name => name.Name == nameof(AccountTransferActivities.RefreshAccountTransfersActivity)),
                It.Is<RefreshAccountTransfersInput>(transferInput =>
                    transferInput.Payments.Count == pageOneLookups.Count * 2 + pageTwoLookups.Count
                    && transferInput.Payments.Count <= StageAccountPaymentsPageResult.MaxTransferLookupsPerPage * 3),
                It.IsAny<TaskOptions>()),
            Times.Once);
    }

    [Test]
    public async Task Then_Continues_When_Refresh_Payment_Data_Completed_Event_Publishing_Fails()
    {
        var input = new ProcessAccountInput
        {
            AccountId = 12345,
            PeriodEndRef = "2024-01",
            CorrelationId = "correlation-id",
            IdempotencyKey = "idempotency-key"
        };

        SetupSingleSucceededPage(input, paymentsCreated: 1);
        _contextMock.Setup(context => context.CallActivityAsync<PublishRefreshPaymentDataCompletedEventResult>(
                It.IsAny<TaskName>(),
                It.IsAny<object>(),
                It.IsAny<TaskOptions>()))
            .ThrowsAsync(new InvalidOperationException("Service Bus publish failed"));
        _contextMock.Setup(context => context.CallActivityAsync<RefreshAccountTransfersResult>(
                It.IsAny<TaskName>(),
                It.IsAny<object>(),
                It.IsAny<TaskOptions>()))
            .ReturnsAsync(new RefreshAccountTransfersResult
            {
                TransfersProcessed = 0,
                Status = "Succeeded",
                Message = "ok"
            });

        var result = await _orchestrator.RunOrchestrator(_contextMock.Object);

        result.Success.Should().BeTrue();
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
    public async Task Then_Returns_Failed_Result_When_Page_Staging_Fails()
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
        _contextMock.Setup(context => context.CallActivityAsync<StageAccountPaymentsPageResult>(
                It.IsAny<TaskName>(),
                It.IsAny<object>(),
                It.IsAny<TaskOptions>()))
            .ReturnsAsync(new StageAccountPaymentsPageResult
            {
                PageNumber = 1,
                TotalPages = 1,
                ItemCount = 1,
                PaymentsCreated = 0,
                TransferLookups = [new TransferPaymentLookup { PaymentId = Guid.NewGuid() }],
                Status = "Failed",
                Message = "Finance API returned BadRequest"
            });
        _contextMock.Setup(context => context.CallActivityAsync<RefreshAccountTransfersResult>(
                It.IsAny<TaskName>(),
                It.IsAny<object>(),
                It.IsAny<TaskOptions>()))
            .ReturnsAsync(new RefreshAccountTransfersResult
            {
                TransfersProcessed = 0,
                Status = "Succeeded",
                Message = "ok"
            });

        var result = await _orchestrator.RunOrchestrator(_contextMock.Object);

        result.Success.Should().BeFalse();
        _contextMock.Verify(context => context.CallActivityAsync<PublishRefreshPaymentDataCompletedEventResult>(
                It.IsAny<TaskName>(),
                It.IsAny<object>(),
                It.IsAny<TaskOptions>()),
            Times.Never);
    }

    [Test]
    public async Task Then_Does_Not_Publish_Completed_Event_When_No_Payments_Were_Created_On_Succeeded_Pages()
    {
        var input = new ProcessAccountInput
        {
            AccountId = 12345,
            PeriodEndRef = "2024-01",
            CorrelationId = "correlation-id",
            IdempotencyKey = "idempotency-key"
        };

        SetupSingleSucceededPage(input, paymentsCreated: 0);
        _contextMock.Setup(context => context.CallActivityAsync<RefreshAccountTransfersResult>(
                It.IsAny<TaskName>(),
                It.IsAny<object>(),
                It.IsAny<TaskOptions>()))
            .ReturnsAsync(new RefreshAccountTransfersResult
            {
                TransfersProcessed = 0,
                Status = "Succeeded",
                Message = "ok"
            });

        var result = await _orchestrator.RunOrchestrator(_contextMock.Object);

        result.Success.Should().BeTrue();
        _contextMock.Verify(context => context.CallActivityAsync<PublishRefreshPaymentDataCompletedEventResult>(
                It.Is<TaskName>(name => name.Name == nameof(RefreshPaymentDataCompletedEventActivities.PublishRefreshPaymentDataCompletedEventActivity)),
                It.Is<PublishRefreshPaymentDataCompletedEventInput>(publishInput => !publishInput.PaymentsProcessed),
                It.IsAny<TaskOptions>()),
            Times.Once);
    }

    private void SetupSingleSucceededPage(ProcessAccountInput input, int paymentsCreated)
    {
        _contextMock.Setup(context => context.GetInput<ProcessAccountInput>())
            .Returns(input);
        _contextMock.Setup(context => context.CallActivityAsync<StageAccountPaymentsPageResult>(
                It.IsAny<TaskName>(),
                It.IsAny<object>(),
                It.IsAny<TaskOptions>()))
            .ReturnsAsync(new StageAccountPaymentsPageResult
            {
                PageNumber = 1,
                TotalPages = 1,
                ItemCount = 1,
                PaymentsCreated = paymentsCreated,
                TransferLookups = [new TransferPaymentLookup { PaymentId = Guid.NewGuid() }],
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

    private void SetupTransferStagedToOperationalSkipped()
    {
        _contextMock.Setup(context => context.CallActivityAsync<TransferStagedToOperationalResult>(
                It.Is<TaskName>(name => name.Name == nameof(TransferStagedToOperationalActivities.TransferStagedToOperationalActivity)),
                It.IsAny<object>(),
                It.IsAny<TaskOptions>()))
            .ReturnsAsync(new TransferStagedToOperationalResult
            {
                TransfersProcessed = 0,
                Status = "Skipped",
                Message = "Transfer staged-to-operational processing is disabled."
            });
    }
}
