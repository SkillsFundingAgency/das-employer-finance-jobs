using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;
using SFA.DAS.Provider.Events.Api.Types;
using ProviderAccountTransfer = SFA.DAS.Provider.Events.Api.Types.AccountTransfer;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Services;

[TestFixture]
public class WhenRefreshingAccountTransfers
{
    private Mock<IProviderPaymentApiClient<ProviderEventsApiConfiguration>> _providerPaymentApiClientMock;
    private Mock<IFinanceApiClient<FinanceApiConfiguration>> _financeApiClientMock;
    private Mock<ILogger<AccountTransfersService>> _loggerMock;
    private AccountTransfersService _service;

    [SetUp]
    public void SetUp()
    {
        _providerPaymentApiClientMock = new Mock<IProviderPaymentApiClient<ProviderEventsApiConfiguration>>();
        _financeApiClientMock = new Mock<IFinanceApiClient<FinanceApiConfiguration>>();
        _loggerMock = new Mock<ILogger<AccountTransfersService>>();
        _service = new AccountTransfersService(
            _providerPaymentApiClientMock.Object,
            _financeApiClientMock.Object,
            _loggerMock.Object);
    }

    [Test]
    public async Task Then_Retrieves_Transfers_And_Posts_Them_To_Finance_Staging()
    {
        var requiredPaymentId = Guid.NewGuid();
        var evidenceSubmittedOn = new DateTime(2025, 11, 18, 15, 22, 44, DateTimeKind.Utc);
        var input = CreateInput([
            CreatePayment(requiredPaymentId, 10000494, evidenceSubmittedOn)
        ]);
        var transfer = CreateTransfer(98765, input.AccountId, requiredPaymentId);
        StageTransfersRequest capturedRequest = null;

        SetupProviderTransfers([transfer]);
        _financeApiClientMock
            .Setup(client => client.PostWithResponseCode<PostTransfersToStagingResponse>(
                It.IsAny<PostTransfersToStagingRequest>()))
            .Callback<IApiRequest>(request => capturedRequest = (StageTransfersRequest)request.Data)
            .ReturnsAsync(new ApiResponse<PostTransfersToStagingResponse>(
                new PostTransfersToStagingResponse { InsertedCount = 1, TransferIds = [transfer.TransferId] },
                HttpStatusCode.Created,
                null));

        var result = await _service.RefreshAccountTransfers(input);

        result.Status.Should().Be("Succeeded");
        result.TransfersProcessed.Should().Be(1);
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Transfers.Should().ContainSingle();
        var stagedTransfer = capturedRequest.Transfers.Single();
        stagedTransfer.TransferId.Should().Be(transfer.TransferId);
        stagedTransfer.SenderAccountId.Should().Be(transfer.SenderAccountId);
        stagedTransfer.ReceiverAccountId.Should().Be(input.AccountId);
        stagedTransfer.ReceiverAccountName.Should().Be(input.AccountName);
        stagedTransfer.Amount.Should().Be(transfer.Amount);
        stagedTransfer.TransferDate.Should().Be(evidenceSubmittedOn);
        stagedTransfer.PeriodEnd.Should().Be(input.PeriodEndRef);
        stagedTransfer.CollectionPeriodMonth.Should().Be(10);
        stagedTransfer.CollectionPeriodYear.Should().Be(2025);
        stagedTransfer.Ukprn.Should().Be(10000494);
        stagedTransfer.CourseName.Should().BeEmpty();
        stagedTransfer.CreatedBy.Should().Be("EmployerFinanceJobs");
        stagedTransfer.CorrelationId.Should().Be(input.CorrelationId);
        _providerPaymentApiClientMock.Verify(client => client.GetWithResponseCode<GetTransfersResponse>(
                It.Is<GetAccountTransfersRequest>(request =>
                    request.GetUrl == $"api/transfers?page=1&periodId={input.PeriodEndRef}&receiverAccountId={input.AccountId}")),
            Times.Once);
    }

    [Test]
    public async Task Then_Does_Not_Call_Finance_When_No_Transfers_Are_Returned()
    {
        var input = CreateInput([]);

        SetupProviderTransfers([]);

        var result = await _service.RefreshAccountTransfers(input);

        result.Status.Should().Be("Succeeded");
        result.TransfersProcessed.Should().Be(0);
        result.Message.Should().Be("No transfers to post into staging.");
        _financeApiClientMock.Verify(
            client => client.PostWithResponseCode<PostTransfersToStagingResponse>(It.IsAny<PostTransfersToStagingRequest>()),
            Times.Never);
    }

    [Test]
    public async Task Then_Treats_Conflict_As_Idempotent_And_Retries_Remaining_Transfers()
    {
        var alreadyStagedPaymentId = Guid.NewGuid();
        var newPaymentId = Guid.NewGuid();
        var alreadyStagedTransfer = CreateTransfer(1001, 12345, alreadyStagedPaymentId);
        var newTransfer = CreateTransfer(1002, 12345, newPaymentId);
        var input = CreateInput([
            CreatePayment(alreadyStagedPaymentId, 10000494, new DateTime(2025, 11, 18, 12, 0, 0, DateTimeKind.Utc)),
            CreatePayment(newPaymentId, 10000495, new DateTime(2025, 11, 18, 13, 0, 0, DateTimeKind.Utc))
        ]);
        var postedTransferBatches = new List<List<long>>();

        SetupProviderTransfers([alreadyStagedTransfer, newTransfer]);
        _financeApiClientMock
            .Setup(client => client.PostWithResponseCode<PostTransfersToStagingResponse>(
                It.IsAny<PostTransfersToStagingRequest>()))
            .Callback<IApiRequest>(request =>
            {
                var stageTransfersRequest = (StageTransfersRequest)request.Data;
                postedTransferBatches.Add(stageTransfersRequest.Transfers.Select(transfer => transfer.TransferId).ToList());
            })
            .ReturnsAsync(() => postedTransferBatches.Count == 1
                ? new ApiResponse<PostTransfersToStagingResponse>(
                    new PostTransfersToStagingResponse(),
                    HttpStatusCode.Conflict,
                    $$"""{"transferIds":[{{alreadyStagedTransfer.TransferId}}]}""")
                : new ApiResponse<PostTransfersToStagingResponse>(
                    new PostTransfersToStagingResponse { InsertedCount = 1, TransferIds = [newTransfer.TransferId] },
                    HttpStatusCode.Created,
                    null));

        var result = await _service.RefreshAccountTransfers(input);

        result.Status.Should().Be("Succeeded");
        result.TransfersProcessed.Should().Be(1);
        result.Message.Should().Be("Successfully staged 1 transfers. 1 transfers already existed in staging.");
        postedTransferBatches.Should().HaveCount(2);
        postedTransferBatches[0].Should().BeEquivalentTo([alreadyStagedTransfer.TransferId, newTransfer.TransferId]);
        postedTransferBatches[1].Should().BeEquivalentTo([newTransfer.TransferId]);
    }

    [Test]
    public async Task Then_Deduplicates_Provider_Transfers_Before_Posting_To_Finance()
    {
        var requiredPaymentId = Guid.NewGuid();
        var transfer = CreateTransfer(1001, 12345, requiredPaymentId);
        var duplicateTransfer = CreateTransfer(1001, 12345, requiredPaymentId);
        var input = CreateInput([CreatePayment(requiredPaymentId, 10000494, DateTime.UtcNow)]);
        StageTransfersRequest capturedRequest = null;

        SetupProviderTransfers([transfer, duplicateTransfer]);
        _financeApiClientMock
            .Setup(client => client.PostWithResponseCode<PostTransfersToStagingResponse>(
                It.IsAny<PostTransfersToStagingRequest>()))
            .Callback<IApiRequest>(request => capturedRequest = (StageTransfersRequest)request.Data)
            .ReturnsAsync(new ApiResponse<PostTransfersToStagingResponse>(
                new PostTransfersToStagingResponse { InsertedCount = 1 },
                HttpStatusCode.Created,
                null));

        var result = await _service.RefreshAccountTransfers(input);

        result.Status.Should().Be("Succeeded");
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Transfers.Should().ContainSingle();
        capturedRequest.Transfers.Single().TransferId.Should().Be(transfer.TransferId);
    }

    [Test]
    public async Task Then_Returns_Failed_When_Finance_Api_Rejects_Staging()
    {
        var requiredPaymentId = Guid.NewGuid();
        var input = CreateInput([CreatePayment(requiredPaymentId, 10000494, DateTime.UtcNow)]);
        var transfer = CreateTransfer(1001, input.AccountId, requiredPaymentId);

        SetupProviderTransfers([transfer]);
        _financeApiClientMock
            .Setup(client => client.PostWithResponseCode<PostTransfersToStagingResponse>(
                It.IsAny<PostTransfersToStagingRequest>()))
            .ReturnsAsync(new ApiResponse<PostTransfersToStagingResponse>(
                new PostTransfersToStagingResponse(),
                HttpStatusCode.BadRequest,
                "[\"TransferId must be greater than 0\"]"));

        var result = await _service.RefreshAccountTransfers(input);

        result.Status.Should().Be("Failed");
        result.TransfersProcessed.Should().Be(0);
        result.Message.Should().Contain("BadRequest");
        result.Message.Should().Contain("TransferId must be greater than 0");
    }

    private void SetupProviderTransfers(IReadOnlyCollection<ProviderAccountTransfer> transfers)
    {
        _providerPaymentApiClientMock
            .Setup(client => client.GetWithResponseCode<GetTransfersResponse>(
                It.IsAny<GetAccountTransfersRequest>()))
            .ReturnsAsync(new ApiResponse<GetTransfersResponse>(
                new GetTransfersResponse
                {
                    PageNumber = 1,
                    TotalNumberOfPages = 1,
                    Items = transfers.ToArray()
                },
                HttpStatusCode.OK,
                null));
    }

    private static RefreshAccountTransfersInput CreateInput(IReadOnlyCollection<Payment> payments)
    {
        return new RefreshAccountTransfersInput
        {
            AccountId = 12345,
            AccountName = "Receiver Account",
            PeriodEndRef = "2526-R03",
            CorrelationId = "correlation-id",
            TriggeredAt = new DateTime(2025, 11, 18, 10, 0, 0, DateTimeKind.Utc),
            Payments = payments
        };
    }

    private static ProviderAccountTransfer CreateTransfer(long transferId, long receiverAccountId, Guid requiredPaymentId)
    {
        return new ProviderAccountTransfer
        {
            TransferId = transferId,
            SenderAccountId = 56789,
            ReceiverAccountId = receiverAccountId,
            RequiredPaymentId = requiredPaymentId,
            Amount = 123.45m,
            CommitmentId = 991122,
            CollectionPeriodName = "2526-R03"
        };
    }

    private static Payment CreatePayment(Guid paymentId, long ukprn, DateTime evidenceSubmittedOn)
    {
        return new Payment
        {
            Id = paymentId.ToString(),
            Ukprn = ukprn,
            EvidenceSubmittedOn = evidenceSubmittedOn,
            CollectionPeriod = new NamedCalendarPeriod
            {
                Id = "2526-R03",
                Month = 10,
                Year = 2025
            }
        };
    }
}
