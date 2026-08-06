using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;
using SFA.DAS.Encoding;
using SFA.DAS.Provider.Events.Api.Types;
using FinanceJobsAccountTransfer = SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models.AccountTransfer;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Services;

[TestFixture]
public class WhenCreatingPaymentTransactionLines
{
    private Mock<IFinanceApiClient<FinanceApiConfiguration>> _financeApiClientMock;
    private Mock<IEncodingService> _encodingServiceMock;
    private Mock<ILogger<PeriodEndService>> _loggerMock;
    private PaymentTransactionLinesService _service;

    [SetUp]
    public void SetUp()
    {
        _financeApiClientMock = new Mock<IFinanceApiClient<FinanceApiConfiguration>>();
        _encodingServiceMock = new Mock<IEncodingService>();
        _loggerMock = new Mock<ILogger<PeriodEndService>>();
        _service = new PaymentTransactionLinesService(_financeApiClientMock.Object, _encodingServiceMock.Object, _loggerMock.Object);
    }

    [Test]
    public async Task Then_Creates_Only_Transaction_Lines_That_Do_Not_Already_Exist()
    {
        const long accountId = 12345;
        const string periodEnd = "2425-R06";
        var matchingEvidenceDate = new DateTime(2024, 01, 31, 12, 0, 0, DateTimeKind.Utc);
        var newEvidenceDate = new DateTime(2024, 01, 31, 13, 0, 0, DateTimeKind.Utc);
        var input = new CreatePaymentTransactionLinesInput
        {
            AccountId = accountId,
            PeriodEnd = periodEnd,
            CorrelationId = "correlation-id",
            PaymentDetails =
            [
                CreatePayment("payment-1", accountId, 1001, 100m, matchingEvidenceDate),
                CreatePayment("payment-2", accountId, 2002, 200m, newEvidenceDate)
            ]
        };

        _encodingServiceMock
            .Setup(service => service.Encode(accountId, EncodingType.AccountId))
            .Returns("ABC123");
        _financeApiClientMock
            .Setup(client => client.GetWithResponseCode<List<PaymentTransactionLine>>(It.IsAny<GetExistinTransactionLinesRequest>()))
            .ReturnsAsync(new ApiResponse<List<PaymentTransactionLine>>(
                [
                    new PaymentTransactionLine
                    {
                        AccountId = accountId,
                        Ukprn = 1001,
                        PeriodEnd = periodEnd,
                        TransactionType = 3,
                        Amount = -100m,
                        SfaCoInvestmentAmount = 0m,
                        EmployerCoInvestmentAmount = 0m,
                        TransactionDate = matchingEvidenceDate
                    }
                ],
                System.Net.HttpStatusCode.OK,
                null));
        _financeApiClientMock
            .Setup(client => client.PostWithResponseCode<PostTransactionLinesToStagingResponse>(
                It.Is<PostTransactionLinesToStagingRequest>(request =>
                    ((TransactionLineStagingRequest)request.Data).TransactionLines.Count == 1
                    && ((TransactionLineStagingRequest)request.Data).TransactionLines[0].Ukprn == 2002)))
            .ReturnsAsync(new ApiResponse<PostTransactionLinesToStagingResponse>(
                new PostTransactionLinesToStagingResponse { InsertedCount = 1 },
                System.Net.HttpStatusCode.Created,
                null));

        var result = await _service.CreatePaymentTransactionLines(input);

        result.TransactionsCreated.Should().Be(1);
        result.Transactions.Should().HaveCount(1);
        result.Transactions.Single().Ukprn.Should().Be(2002);
        result.Transactions.Single().AccountId.Should().Be(accountId);
        result.Transactions.Single().PeriodEnd.Should().Be(periodEnd);
    }

    [Test]
    public async Task Then_Returns_Empty_Result_When_All_Transaction_Lines_Already_Exist()
    {
        const long accountId = 12345;
        const string periodEnd = "2024-01";
        var evidenceDate = new DateTime(2024, 01, 31, 12, 0, 0, DateTimeKind.Utc);
        var input = new CreatePaymentTransactionLinesInput
        {
            AccountId = accountId,
            PeriodEnd = periodEnd,
            CorrelationId = "correlation-id",
            PaymentDetails =
            [
                CreatePayment("payment-1", accountId, 1001, 100m, evidenceDate)
            ]
        };

        _encodingServiceMock
            .Setup(service => service.Encode(accountId, EncodingType.AccountId))
            .Returns("ABC123");
        _financeApiClientMock
            .Setup(client => client.GetWithResponseCode<List<PaymentTransactionLine>>(It.IsAny<GetExistinTransactionLinesRequest>()))
            .ReturnsAsync(new ApiResponse<List<PaymentTransactionLine>>(
                [
                    new PaymentTransactionLine
                    {
                        AccountId = accountId,
                        Ukprn = 1001,
                        PeriodEnd = periodEnd,
                        TransactionType = 3,
                        Amount = -100m,
                        SfaCoInvestmentAmount = 0m,
                        EmployerCoInvestmentAmount = 0m,
                        TransactionDate = evidenceDate
                    }
                ],
                System.Net.HttpStatusCode.OK,
                null));

        var result = await _service.CreatePaymentTransactionLines(input);

        result.TransactionsCreated.Should().Be(0);
        result.Transactions.Should().BeEmpty();
        _financeApiClientMock.Verify(
            client => client.PostWithResponseCode<PostTransactionLinesToStagingResponse>(It.IsAny<PostTransactionLinesToStagingRequest>()),
            Times.Never);
    }

    [Test]
    public async Task Then_Uses_The_Processing_Account_Id_And_Period_End_For_Staging()
    {
        const long processingAccountId = 594;
        const long providerEmployerAccountId = 999999;
        const string periodEnd = "2425-R06";
        var evidenceDate = new DateTime(2026, 05, 28, 10, 33, 37, DateTimeKind.Utc);
        TransactionLineStagingRequest capturedRequest = null;
        var input = new CreatePaymentTransactionLinesInput
        {
            AccountId = processingAccountId,
            PeriodEnd = periodEnd,
            CorrelationId = "correlation-id",
            PaymentDetails =
            [
                CreatePayment("payment-1", providerEmployerAccountId, 1001, 100m, evidenceDate)
            ]
        };

        _encodingServiceMock
            .Setup(service => service.Encode(processingAccountId, EncodingType.AccountId))
            .Returns("ABC123");
        _financeApiClientMock
            .Setup(client => client.GetWithResponseCode<List<PaymentTransactionLine>>(It.IsAny<GetExistinTransactionLinesRequest>()))
            .ReturnsAsync(new ApiResponse<List<PaymentTransactionLine>>(
                [],
                System.Net.HttpStatusCode.OK,
                null));
        _financeApiClientMock
            .Setup(client => client.PostWithResponseCode<PostTransactionLinesToStagingResponse>(
                It.IsAny<PostTransactionLinesToStagingRequest>()))
            .Callback<IApiRequest>(request => capturedRequest = (TransactionLineStagingRequest)request.Data)
            .ReturnsAsync(new ApiResponse<PostTransactionLinesToStagingResponse>(
                new PostTransactionLinesToStagingResponse { InsertedCount = 1 },
                System.Net.HttpStatusCode.Created,
                null));

        var result = await _service.CreatePaymentTransactionLines(input);

        result.TransactionsCreated.Should().Be(1);
        capturedRequest.Should().NotBeNull();
        capturedRequest!.TransactionLines.Should().ContainSingle();
        var transactionLine = capturedRequest.TransactionLines.Single();
        transactionLine.AccountId.Should().Be(processingAccountId);
        transactionLine.PeriodEnd.Should().Be(periodEnd);
        transactionLine.DateCreated.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Test]
    public async Task Then_Rounds_Transaction_Line_Amounts_To_Staging_Table_Precision()
    {
        const long accountId = 14331;
        const string periodEnd = "2526-R03";
        var evidenceDate = new DateTime(2025, 11, 18, 15, 22, 44, DateTimeKind.Utc);
        TransactionLineStagingRequest capturedRequest = null;
        var input = new CreatePaymentTransactionLinesInput
        {
            AccountId = accountId,
            PeriodEnd = periodEnd,
            CorrelationId = "correlation-id",
            PaymentDetails =
            [
                CreatePayment("payment-1", accountId, 10000494, 631.57895m, evidenceDate)
            ]
        };

        _encodingServiceMock
            .Setup(service => service.Encode(accountId, EncodingType.AccountId))
            .Returns("ABC123");
        _financeApiClientMock
            .Setup(client => client.GetWithResponseCode<List<PaymentTransactionLine>>(It.IsAny<GetExistinTransactionLinesRequest>()))
            .ReturnsAsync(new ApiResponse<List<PaymentTransactionLine>>(
                [],
                System.Net.HttpStatusCode.OK,
                null));
        _financeApiClientMock
            .Setup(client => client.PostWithResponseCode<PostTransactionLinesToStagingResponse>(
                It.IsAny<PostTransactionLinesToStagingRequest>()))
            .Callback<IApiRequest>(request => capturedRequest = (TransactionLineStagingRequest)request.Data)
            .ReturnsAsync(new ApiResponse<PostTransactionLinesToStagingResponse>(
                new PostTransactionLinesToStagingResponse { InsertedCount = 1 },
                System.Net.HttpStatusCode.Created,
                null));

        await _service.CreatePaymentTransactionLines(input);

        capturedRequest.Should().NotBeNull();
        var transactionLine = capturedRequest!.TransactionLines.Single();
        transactionLine.Amount.Should().Be(-631.5790m);
        transactionLine.SfaCoInvestmentAmount.Should().Be(0m);
        transactionLine.EmployerCoInvestmentAmount.Should().Be(0m);
    }

    [Test]
    public void Then_Throws_When_Transaction_Lines_Cannot_Be_Built()
    {
        var input = new CreatePaymentTransactionLinesInput
        {
            AccountId = 594,
            PeriodEnd = "2425-R06",
            CorrelationId = "correlation-id",
            PaymentDetails =
            [
                new Payment
                {
                    Id = Guid.NewGuid().ToString(),
                    EmployerAccountId = "594",
                    Ukprn = 1001,
                    FundingSource = FundingSource.Levy,
                    Amount = 100m,
                    EvidenceSubmittedOn = new DateTime(2026, 05, 28, 10, 33, 37, DateTimeKind.Utc)
                }
            ]
        };

        Assert.ThrowsAsync<NullReferenceException>(() => _service.CreatePaymentTransactionLines(input));
    }

    [Test]
    public void Then_Throws_When_Finance_Api_Rejects_Transaction_Line_Staging()
    {
        var input = new CreatePaymentTransactionLinesInput
        {
            AccountId = 594,
            PeriodEnd = "2425-R06",
            CorrelationId = "correlation-id",
            PaymentDetails =
            [
                CreatePayment("payment-1", 594, 1001, 100m, new DateTime(2026, 05, 28, 10, 33, 37, DateTimeKind.Utc))
            ]
        };

        _encodingServiceMock
            .Setup(service => service.Encode(input.AccountId, EncodingType.AccountId))
            .Returns("ABC123");
        _financeApiClientMock
            .Setup(client => client.GetWithResponseCode<List<PaymentTransactionLine>>(It.IsAny<GetExistinTransactionLinesRequest>()))
            .ReturnsAsync(new ApiResponse<List<PaymentTransactionLine>>(
                [],
                System.Net.HttpStatusCode.OK,
                null));
        _financeApiClientMock
            .Setup(client => client.PostWithResponseCode<PostTransactionLinesToStagingResponse>(
                It.IsAny<PostTransactionLinesToStagingRequest>()))
            .ReturnsAsync(new ApiResponse<PostTransactionLinesToStagingResponse>(
                new PostTransactionLinesToStagingResponse(),
                System.Net.HttpStatusCode.BadRequest,
                "[\"AccountId is mandatory and must be > 0.\"]"));

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreatePaymentTransactionLines(input));
        exception!.Message.Should().Contain("BadRequest");
        exception.Message.Should().Contain("AccountId is mandatory");
    }

    [Test]
    public async Task Then_Creates_Sender_And_Receiver_Transfer_Transaction_Lines_For_A_Single_Sender()
    {
        const string periodEnd = "2526-R03";
        const long senderAccountId = 12345;
        const long receiverAccountId = 67890;
        const string senderAccountName = "Sender Account";
        const string receiverAccountName = "Receiver Account";
        TransactionLineStagingRequest capturedRequest = null;
        var input = new CreateTransferTransactionLinesInput
        {
            PeriodEnd = periodEnd,
            CorrelationId = "correlation-id",
            Transfers =
            [
                CreateTransfer(senderAccountId, senderAccountName, receiverAccountId, receiverAccountName, periodEnd, 100m),
                CreateTransfer(senderAccountId, senderAccountName, receiverAccountId, receiverAccountName, periodEnd, 200m)
            ]
        };

        _financeApiClientMock
            .Setup(client => client.PostWithResponseCode<PostTransactionLinesToStagingResponse>(
                It.IsAny<PostTransactionLinesToStagingRequest>()))
            .Callback<IApiRequest>(request => capturedRequest = (TransactionLineStagingRequest)request.Data)
            .ReturnsAsync(new ApiResponse<PostTransactionLinesToStagingResponse>(
                new PostTransactionLinesToStagingResponse { InsertedCount = 2 },
                System.Net.HttpStatusCode.Created,
                null));

        var result = await _service.CreateTransferTransactionLines(input);

        result.Status.Should().Be("Succeeded");
        result.TransactionsCreated.Should().Be(2);
        capturedRequest.Should().NotBeNull();
        capturedRequest!.TransactionLines.Should().HaveCount(2);

        var senderLine = capturedRequest.TransactionLines.Single(line => line.AccountId == senderAccountId);
        senderLine.TransactionType.Should().Be(4);
        senderLine.Amount.Should().Be(-300m);
        senderLine.PeriodEnd.Should().Be(periodEnd);
        senderLine.TransferSenderAccountId.Should().Be(senderAccountId);
        senderLine.TransferSenderAccountName.Should().Be(senderAccountName);
        senderLine.TransferReceiverAccountId.Should().Be(receiverAccountId);
        senderLine.TransferReceiverAccountName.Should().Be(receiverAccountName);

        var receiverLine = capturedRequest.TransactionLines.Single(line => line.AccountId == receiverAccountId);
        receiverLine.TransactionType.Should().Be(4);
        receiverLine.Amount.Should().Be(300m);
        receiverLine.TransferSenderAccountId.Should().Be(senderAccountId);
        receiverLine.TransferSenderAccountName.Should().Be(senderAccountName);
        receiverLine.TransferReceiverAccountId.Should().Be(receiverAccountId);
        receiverLine.TransferReceiverAccountName.Should().Be(receiverAccountName);

        _financeApiClientMock.Verify(
            client => client.GetWithResponseCode<List<PaymentTransactionLine>>(It.IsAny<GetExistinTransactionLinesRequest>()),
            Times.Never);
    }

    [Test]
    public async Task Then_Creates_Transfer_Transaction_Line_Pairs_For_Multiple_Senders()
    {
        const string periodEnd = "2526-R03";
        const long receiverAccountId = 67890;
        TransactionLineStagingRequest capturedRequest = null;
        var input = new CreateTransferTransactionLinesInput
        {
            PeriodEnd = periodEnd,
            CorrelationId = "correlation-id",
            Transfers =
            [
                CreateTransfer(10001, "Sender One", receiverAccountId, "Receiver Account", periodEnd, 100m),
                CreateTransfer(10001, "Sender One", receiverAccountId, "Receiver Account", periodEnd, 200m),
                CreateTransfer(10002, "Sender Two", receiverAccountId, "Receiver Account", periodEnd, 400m)
            ]
        };

        _financeApiClientMock
            .Setup(client => client.PostWithResponseCode<PostTransactionLinesToStagingResponse>(
                It.IsAny<PostTransactionLinesToStagingRequest>()))
            .Callback<IApiRequest>(request => capturedRequest = (TransactionLineStagingRequest)request.Data)
            .ReturnsAsync(new ApiResponse<PostTransactionLinesToStagingResponse>(
                new PostTransactionLinesToStagingResponse { InsertedCount = 4 },
                System.Net.HttpStatusCode.Created,
                null));

        var result = await _service.CreateTransferTransactionLines(input);

        result.Status.Should().Be("Succeeded");
        result.TransactionsCreated.Should().Be(4);
        capturedRequest.Should().NotBeNull();
        capturedRequest!.TransactionLines.Should().HaveCount(4);
        capturedRequest.TransactionLines.Should().OnlyContain(line => line.TransactionType == 4);

        capturedRequest.TransactionLines.Should().Contain(line =>
            line.AccountId == 10001
            && line.Amount == -300m
            && line.TransferSenderAccountId == 10001
            && line.TransferReceiverAccountId == receiverAccountId);
        capturedRequest.TransactionLines.Should().Contain(line =>
            line.AccountId == receiverAccountId
            && line.Amount == 300m
            && line.TransferSenderAccountId == 10001
            && line.TransferReceiverAccountId == receiverAccountId);
        capturedRequest.TransactionLines.Should().Contain(line =>
            line.AccountId == 10002
            && line.Amount == -400m
            && line.TransferSenderAccountId == 10002
            && line.TransferReceiverAccountId == receiverAccountId);
        capturedRequest.TransactionLines.Should().Contain(line =>
            line.AccountId == receiverAccountId
            && line.Amount == 400m
            && line.TransferSenderAccountId == 10002
            && line.TransferReceiverAccountId == receiverAccountId);
    }

    [Test]
    public async Task Then_Does_Not_Post_Transfer_Transaction_Lines_When_There_Are_No_Transfers()
    {
        var input = new CreateTransferTransactionLinesInput
        {
            PeriodEnd = "2526-R03",
            CorrelationId = "correlation-id",
            Transfers = []
        };

        var result = await _service.CreateTransferTransactionLines(input);

        result.Status.Should().Be("Succeeded");
        result.TransactionsCreated.Should().Be(0);
        result.Transactions.Should().BeEmpty();
        _financeApiClientMock.Verify(
            client => client.PostWithResponseCode<PostTransactionLinesToStagingResponse>(It.IsAny<PostTransactionLinesToStagingRequest>()),
            Times.Never);
    }

    private static Payment CreatePayment(string paymentId, long accountId, long ukprn, decimal amount, DateTime evidenceSubmittedOn)
    {
        return new Payment
        {
            Id = paymentId,
            EmployerAccountId = accountId.ToString(),
            Ukprn = ukprn,
            FundingSource = (FundingSource)1,
            Amount = amount,
            EvidenceSubmittedOn = evidenceSubmittedOn,
            CollectionPeriod = new NamedCalendarPeriod
            {
                Id = "R01",
                Month = 1,
                Year = 2024
            }
        };
    }

    private static FinanceJobsAccountTransfer CreateTransfer(
        long senderAccountId,
        string senderAccountName,
        long receiverAccountId,
        string receiverAccountName,
        string periodEnd,
        decimal amount)
    {
        return new FinanceJobsAccountTransfer
        {
            SenderAccountId = senderAccountId,
            SenderAccountName = senderAccountName,
            ReceiverAccountId = receiverAccountId,
            ReceiverAccountName = receiverAccountName,
            PeriodEnd = periodEnd,
            Amount = amount
        };
    }

}
