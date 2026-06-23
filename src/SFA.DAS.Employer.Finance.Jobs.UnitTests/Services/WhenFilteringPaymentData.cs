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

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Services;

[TestFixture]
public class WhenFilteringPaymentData
{
    private Mock<IFinanceApiClient<FinanceApiConfiguration>> _financeApiClientMock;
    private Mock<ILogger<RefreshPaymentDataService>> _loggerMock;
    private RefreshPaymentDataService _service;

    [SetUp]
    public void SetUp()
    {
        _financeApiClientMock = new Mock<IFinanceApiClient<FinanceApiConfiguration>>();
        _loggerMock = new Mock<ILogger<RefreshPaymentDataService>>();
        _service = new RefreshPaymentDataService(_financeApiClientMock.Object, _loggerMock.Object);
    }

    [Test]
    public void Then_Uses_The_Processing_Account_Id_For_Staging()
    {
        const long processingAccountId = 14331;
        var payment = CreatePayment(FundingSource.Levy);

        var result = _service.FilterPayments([payment], [], processingAccountId, "correlation-id");

        result.Should().HaveCount(1);
        result.Single().AccountId.Should().Be(processingAccountId);
    }

    [Test]
    public void Then_Maps_Provider_Payment_To_Staging_Payload()
    {
        const long processingAccountId = 14331;
        var evidenceSubmittedOn = new DateTime(2025, 11, 18, 15, 22, 44, DateTimeKind.Utc);
        var paymentId = Guid.NewGuid();
        var payment = CreatePayment(FundingSource.Levy);
        payment.Id = paymentId.ToString();
        payment.Ukprn = 10000494;
        payment.Uln = 9908090305;
        payment.EmployerAccountId = "999999";
        payment.ApprenticeshipId = 55004;
        payment.Amount = 631.57895m;
        payment.EvidenceSubmittedOn = evidenceSubmittedOn;
        payment.EmployerAccountVersion = "provider-employer-version";
        payment.ApprenticeshipVersion = "provider-apprenticeship-version";
        payment.CollectionPeriod = new NamedCalendarPeriod
        {
            Id = "2526-R03",
            Month = 10,
            Year = 2025
        };
        payment.DeliveryPeriod = new SFA.DAS.Provider.Events.Api.Types.CalendarPeriod
        {
            Month = 10,
            Year = 2025
        };

        var result = _service.FilterPayments([payment], [], processingAccountId, "correlation-id");

        result.Should().ContainSingle();
        var stagingPayment = result.Single();
        stagingPayment.PaymentId.Should().Be(paymentId);
        stagingPayment.AccountId.Should().Be(processingAccountId);
        stagingPayment.Ukprn.Should().Be(payment.Ukprn);
        stagingPayment.Uln.Should().Be(payment.Uln);
        stagingPayment.ApprenticeshipId.Should().Be(payment.ApprenticeshipId);
        stagingPayment.CollectionPeriodId.Should().Be(payment.CollectionPeriod.Id);
        stagingPayment.CollectionPeriodMonth.Should().Be(payment.CollectionPeriod.Month);
        stagingPayment.CollectionPeriodYear.Should().Be(payment.CollectionPeriod.Year);
        stagingPayment.DeliveryPeriodMonth.Should().Be(payment.DeliveryPeriod.Month);
        stagingPayment.DeliveryPeriodYear.Should().Be(payment.DeliveryPeriod.Year);
        stagingPayment.FundingSource.Should().Be(payment.FundingSource.ToString());
        stagingPayment.TransactionType.Should().Be(payment.TransactionType.ToString());
        stagingPayment.Amount.Should().Be(payment.Amount);
        stagingPayment.EvidenceSubmittedOn.Should().Be(evidenceSubmittedOn);
        stagingPayment.EmployerAccountVersion.Should().Be(payment.EmployerAccountVersion);
        stagingPayment.ApprenticeshipVersion.Should().Be(payment.ApprenticeshipVersion);
    }

    [Test]
    public void Then_Removes_Existing_And_Fully_Funded_Sfa_Payments()
    {
        const long processingAccountId = 14331;
        var existingPayment = CreatePayment(FundingSource.Levy);
        var fullyFundedPayment = CreatePayment(FundingSource.FullyFundedSfa);
        var newPayment = CreatePayment(FundingSource.Levy);

        var result = _service.FilterPayments(
            [existingPayment, fullyFundedPayment, newPayment],
            [existingPayment.Id!],
            processingAccountId,
            "correlation-id");

        result.Should().ContainSingle();
        result.Single().PaymentId.Should().Be(Guid.Parse(newPayment.Id!));
    }

    [Test]
    public async Task Then_Logs_An_Exception_When_Finance_Api_Rejects_Staging()
    {
        const string errorContent = "[\"AccountId is mandatory and must be > 0.\"]";

        _financeApiClientMock
            .Setup(client => client.PostWithResponseCode<PostPaymentsToStagingResponse>(
                It.IsAny<PostPaymentsToStagingRequest>()))
            .ReturnsAsync(new ApiResponse<PostPaymentsToStagingResponse>(
                new PostPaymentsToStagingResponse(),
                HttpStatusCode.BadRequest,
                errorContent));

        var result = await _service.PostPaymentsToStaging([new PaymentStaging()], "correlation-id");

        result.Status.Should().Be("Failed");
        result.Message.Should().Contain("BadRequest");
        result.Message.Should().Contain("AccountId is mandatory");
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("Finance API returned BadRequest")),
                It.Is<InvalidOperationException>(exception => exception.Message.Contains("AccountId is mandatory")),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Test]
    public async Task Then_Treats_Created_Response_As_Success_When_Posting_To_Staging()
    {
        _financeApiClientMock
            .Setup(client => client.PostWithResponseCode<PostPaymentsToStagingResponse>(
                It.IsAny<PostPaymentsToStagingRequest>()))
            .ReturnsAsync(new ApiResponse<PostPaymentsToStagingResponse>(
                new PostPaymentsToStagingResponse { InsertedCount = 1 },
                HttpStatusCode.Created,
                null));

        var result = await _service.PostPaymentsToStaging([new PaymentStaging()], "correlation-id");

        result.Status.Should().Be("Succeeded");
        result.PaymentsCreated.Should().Be(1);
    }

    [Test]
    public async Task Then_Fails_When_Finance_Api_Accepts_The_Request_But_Does_Not_Insert_Requested_Payments()
    {
        _financeApiClientMock
            .Setup(client => client.PostWithResponseCode<PostPaymentsToStagingResponse>(
                It.IsAny<PostPaymentsToStagingRequest>()))
            .ReturnsAsync(new ApiResponse<PostPaymentsToStagingResponse>(
                new PostPaymentsToStagingResponse { InsertedCount = 0 },
                HttpStatusCode.Created,
                null));

        var result = await _service.PostPaymentsToStaging(
            [new PaymentStaging { PaymentId = Guid.NewGuid(), AccountId = 594 }],
            "correlation-id");

        result.Status.Should().Be("Failed");
        result.PaymentsCreated.Should().Be(0);
        result.Message.Should().Contain("inserted 0 of 1 requested payments");
    }

    [Test]
    public async Task Then_Treats_Conflict_As_Success_When_All_Payments_Are_Already_Staged()
    {
        var paymentId = Guid.NewGuid();

        _financeApiClientMock
            .Setup(client => client.PostWithResponseCode<PostPaymentsToStagingResponse>(
                It.IsAny<PostPaymentsToStagingRequest>()))
            .ReturnsAsync(new ApiResponse<PostPaymentsToStagingResponse>(
                new PostPaymentsToStagingResponse(),
                HttpStatusCode.Conflict,
                $$"""{"paymentIds":["{{paymentId}}"]}"""));

        var result = await _service.PostPaymentsToStaging(
            [new PaymentStaging { PaymentId = paymentId }],
            "correlation-id");

        result.Status.Should().Be("Succeeded");
        result.PaymentsCreated.Should().Be(0);
        result.Message.Should().Be("Successfully upserted 0 payments to staging. 1 payments already existed in staging.");
    }

    [Test]
    public async Task Then_Retries_Non_Conflicting_Payments_When_Only_Some_Payments_Are_Already_Staged()
    {
        var alreadyStagedPaymentId = Guid.NewGuid();
        var newPaymentId = Guid.NewGuid();
        var postedPaymentBatches = new List<List<Guid>>();

        _financeApiClientMock
            .Setup(client => client.PostWithResponseCode<PostPaymentsToStagingResponse>(
                It.IsAny<PostPaymentsToStagingRequest>()))
            .Callback<IApiRequest>(request =>
            {
                var bulkPaymentsRequest = (BulkPaymentsRequest)request.Data;
                postedPaymentBatches.Add(bulkPaymentsRequest.Payments.Select(payment => payment.PaymentId).ToList());
            })
            .ReturnsAsync(() => postedPaymentBatches.Count == 1
                ? new ApiResponse<PostPaymentsToStagingResponse>(
                    new PostPaymentsToStagingResponse(),
                    HttpStatusCode.Conflict,
                    $$"""{"paymentIds":["{{alreadyStagedPaymentId}}"]}""")
                : new ApiResponse<PostPaymentsToStagingResponse>(
                    new PostPaymentsToStagingResponse { InsertedCount = 1 },
                    HttpStatusCode.Created,
                    null));

        var result = await _service.PostPaymentsToStaging(
            [
                new PaymentStaging { PaymentId = alreadyStagedPaymentId },
                new PaymentStaging { PaymentId = newPaymentId }
            ],
            "correlation-id");

        result.Status.Should().Be("Succeeded");
        result.PaymentsCreated.Should().Be(1);
        postedPaymentBatches.Should().HaveCount(2);
        postedPaymentBatches[0].Should().BeEquivalentTo([alreadyStagedPaymentId, newPaymentId]);
        postedPaymentBatches[1].Should().BeEquivalentTo([newPaymentId]);
    }

    private static Payment CreatePayment(FundingSource fundingSource)
    {
        return new Payment
        {
            Id = Guid.NewGuid().ToString(),
            Ukprn = 10000001,
            Uln = 1234567890,
            ApprenticeshipId = 123,
            CollectionPeriod = new NamedCalendarPeriod
            {
                Id = "2526-R03",
                Month = 10,
                Year = 2025
            },
            DeliveryPeriod = new SFA.DAS.Provider.Events.Api.Types.CalendarPeriod
            {
                Month = 10,
                Year = 2025
            },
            FundingSource = fundingSource,
            Amount = 10m,
            EvidenceSubmittedOn = new DateTime(2025, 11, 18, 15, 22, 44, DateTimeKind.Utc)
        };
    }
}
