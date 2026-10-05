using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;
using SFA.DAS.Provider.Events.Api.Types;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Services;

[TestFixture]
public class WhenStagingAccountPaymentsPage
{
    private Mock<IProviderPaymentApiClient<ProviderEventsApiConfiguration>> _providerEventsClientMock;
    private Mock<IAccountPaymentsImportService> _accountPaymentsImportServiceMock;
    private Mock<IRefreshPaymentDataService> _refreshPaymentDataServiceMock;
    private Mock<IPaymentMetadataService> _paymentMetadataServiceMock;
    private Mock<IPaymentTransactionLinesService> _paymentTransactionLinesServiceMock;
    private Mock<ILogger<AccountPaymentPageStagingService>> _loggerMock;
    private AccountPaymentPageStagingService _service;

    [SetUp]
    public void SetUp()
    {
        _providerEventsClientMock = new Mock<IProviderPaymentApiClient<ProviderEventsApiConfiguration>>();
        _accountPaymentsImportServiceMock = new Mock<IAccountPaymentsImportService>();
        _refreshPaymentDataServiceMock = new Mock<IRefreshPaymentDataService>();
        _paymentMetadataServiceMock = new Mock<IPaymentMetadataService>();
        _paymentTransactionLinesServiceMock = new Mock<IPaymentTransactionLinesService>();
        _loggerMock = new Mock<ILogger<AccountPaymentPageStagingService>>();
        _service = new AccountPaymentPageStagingService(
            _providerEventsClientMock.Object,
            _accountPaymentsImportServiceMock.Object,
            _refreshPaymentDataServiceMock.Object,
            _paymentMetadataServiceMock.Object,
            _paymentTransactionLinesServiceMock.Object,
            _loggerMock.Object);
    }

    [Test]
    public async Task Then_Skips_Existing_Id_Load_And_Downstream_Work_When_Page_Is_Empty()
    {
        _providerEventsClientMock
            .Setup(client => client.GetWithResponseCode<GetPaymentsResponse>(It.IsAny<GetAccountPaymentsRequest>()))
            .ReturnsAsync(new ApiResponse<GetPaymentsResponse>(
                new GetPaymentsResponse
                {
                    PageNumber = 1,
                    TotalNumberOfPages = 0,
                    Items = []
                },
                System.Net.HttpStatusCode.OK,
                null));

        var result = await _service.StageAccountPaymentsPageAsync(CreateInput(), CancellationToken.None);

        result.Status.Should().Be("Succeeded");
        result.ItemCount.Should().Be(0);
        result.TransferLookups.Should().BeEmpty();
        _accountPaymentsImportServiceMock.Verify(
            service => service.ImportAccountExistingPaymentIdsAsync(It.IsAny<long>(), It.IsAny<string>()),
            Times.Never);
        _refreshPaymentDataServiceMock.Verify(
            service => service.FilterPayments(It.IsAny<List<Payment>>(), It.IsAny<List<string>>(), It.IsAny<long>(), It.IsAny<string>()),
            Times.Never);
        _paymentMetadataServiceMock.Verify(
            service => service.CreatePaymentMetadata(It.IsAny<CreatePaymentMetadataInput>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task Then_Stages_Filters_And_Returns_Only_Slim_Transfer_Lookups()
    {
        var paymentId = Guid.NewGuid();
        var payment = new Payment
        {
            Id = paymentId.ToString(),
            Ukprn = 10001234,
            EvidenceSubmittedOn = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            CollectionPeriod = new NamedCalendarPeriod { Month = 8, Year = 2025 },
            ApprenticeshipId = 99
        };

        _providerEventsClientMock
            .Setup(client => client.GetWithResponseCode<GetPaymentsResponse>(It.IsAny<GetAccountPaymentsRequest>()))
            .ReturnsAsync(new ApiResponse<GetPaymentsResponse>(
                new GetPaymentsResponse
                {
                    PageNumber = 1,
                    TotalNumberOfPages = 1,
                    Items = [payment]
                },
                System.Net.HttpStatusCode.OK,
                null));
        _accountPaymentsImportServiceMock
            .Setup(service => service.ImportAccountExistingPaymentIdsAsync(14331, "correlation-id"))
            .ReturnsAsync(new AccountExistingPaymentIdsImportResult
            {
                PaymentIds = [],
                Status = "Succeeded",
                Message = "ok"
            });
        _refreshPaymentDataServiceMock
            .Setup(service => service.FilterPayments(It.IsAny<List<Payment>>(), It.IsAny<List<string>>(), 14331, "correlation-id"))
            .Returns([
                new PaymentStaging
                {
                    PaymentId = paymentId,
                    AccountId = 14331
                }
            ]);
        _refreshPaymentDataServiceMock
            .Setup(service => service.PostPaymentsToStaging(It.IsAny<List<PaymentStaging>>(), "correlation-id"))
            .ReturnsAsync(new RefreshPaymentDataResult
            {
                PaymentsCreated = 1,
                Status = "Succeeded",
                Message = "ok"
            });
        _paymentMetadataServiceMock
            .Setup(service => service.CreatePaymentMetadata(It.IsAny<CreatePaymentMetadataInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreatePaymentMetadataResult
            {
                MetadataCreated = 1,
                Status = "Succeeded",
                Message = "ok"
            });
        _paymentTransactionLinesServiceMock
            .Setup(service => service.CreatePaymentTransactionLines(It.IsAny<CreatePaymentTransactionLinesInput>()))
            .ReturnsAsync(new CreatePaymentTransactionLinesResult
            {
                TransactionsCreated = 1,
                Transactions = [],
                Status = "Succeeded",
                Message = "ok"
            });

        var result = await _service.StageAccountPaymentsPageAsync(CreateInput(), CancellationToken.None);

        result.Status.Should().Be("Succeeded");
        result.ItemCount.Should().Be(1);
        result.PaymentsCreated.Should().Be(1);
        result.MetadataCreated.Should().Be(1);
        result.TransactionsCreated.Should().Be(1);
        result.TransferLookups.Should().HaveCount(1);
        result.TransferLookups.Single().PaymentId.Should().Be(paymentId);
        result.GetType().GetProperty("PaymentDetails").Should().BeNull();
        result.GetType().GetProperty("PaymentIds").Should().BeNull();
        result.TransferLookups.Count.Should().BeLessThanOrEqualTo(StageAccountPaymentsPageResult.MaxTransferLookupsPerPage);

        _paymentMetadataServiceMock.Verify(
            service => service.CreatePaymentMetadata(
                It.Is<CreatePaymentMetadataInput>(metadataInput =>
                    metadataInput.PaymentDetails.Count == 1
                    && metadataInput.PaymentDetails.Single().Id == payment.Id),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Then_Does_Not_Create_Metadata_Or_Transactions_When_No_New_Payments()
    {
        var paymentId = Guid.NewGuid();
        var payment = new Payment { Id = paymentId.ToString() };

        _providerEventsClientMock
            .Setup(client => client.GetWithResponseCode<GetPaymentsResponse>(It.IsAny<GetAccountPaymentsRequest>()))
            .ReturnsAsync(new ApiResponse<GetPaymentsResponse>(
                new GetPaymentsResponse
                {
                    PageNumber = 1,
                    TotalNumberOfPages = 1,
                    Items = [payment]
                },
                System.Net.HttpStatusCode.OK,
                null));
        _accountPaymentsImportServiceMock
            .Setup(service => service.ImportAccountExistingPaymentIdsAsync(14331, "correlation-id"))
            .ReturnsAsync(new AccountExistingPaymentIdsImportResult
            {
                PaymentIds = [paymentId.ToString()],
                Status = "Succeeded",
                Message = "ok"
            });
        _refreshPaymentDataServiceMock
            .Setup(service => service.FilterPayments(It.IsAny<List<Payment>>(), It.IsAny<List<string>>(), 14331, "correlation-id"))
            .Returns([]);

        var result = await _service.StageAccountPaymentsPageAsync(CreateInput(), CancellationToken.None);

        result.Status.Should().Be("Succeeded");
        result.PaymentsCreated.Should().Be(0);
        result.TransferLookups.Should().HaveCount(1);
        _refreshPaymentDataServiceMock.Verify(
            service => service.PostPaymentsToStaging(It.IsAny<List<PaymentStaging>>(), It.IsAny<string>()),
            Times.Never);
        _paymentMetadataServiceMock.Verify(
            service => service.CreatePaymentMetadata(It.IsAny<CreatePaymentMetadataInput>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _paymentTransactionLinesServiceMock.Verify(
            service => service.CreatePaymentTransactionLines(It.IsAny<CreatePaymentTransactionLinesInput>()),
            Times.Never);
    }

    [Test]
    public async Task Then_Fails_When_Provider_Events_Page_Exceeds_Bound()
    {
        var payments = Enumerable.Range(0, StageAccountPaymentsPageResult.MaxTransferLookupsPerPage + 1)
            .Select(_ => new Payment { Id = Guid.NewGuid().ToString() })
            .ToArray();

        _providerEventsClientMock
            .Setup(client => client.GetWithResponseCode<GetPaymentsResponse>(It.IsAny<GetAccountPaymentsRequest>()))
            .ReturnsAsync(new ApiResponse<GetPaymentsResponse>(
                new GetPaymentsResponse
                {
                    PageNumber = 1,
                    TotalNumberOfPages = 1,
                    Items = payments
                },
                System.Net.HttpStatusCode.OK,
                null));

        var act = async () => await _service.StageAccountPaymentsPageAsync(CreateInput(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{StageAccountPaymentsPageResult.MaxTransferLookupsPerPage}*");
    }

    private static StageAccountPaymentsPageInput CreateInput()
    {
        return new StageAccountPaymentsPageInput
        {
            AccountId = 14331,
            AccountName = "Test Account",
            PeriodEndRef = "2526-R01",
            CorrelationId = "correlation-id",
            IdempotencyKey = "idempotency-key",
            TriggeredAt = DateTime.UtcNow,
            PageNumber = 1
        };
    }
}
