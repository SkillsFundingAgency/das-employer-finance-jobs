using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Functions.Activities;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;


namespace SFA.DAS.Employer.Finance.Jobs.Tests.Functions.Activities
{
    [TestFixture]
    public class RefreshPaymentDataActivityTests
    {
        private Mock<IProviderPaymentApiClient<ProviderEventsApiConfiguration>> _providerApi;
        private Mock<IFinanceApiClient<FinanceApiConfiguration>> _financeApi;
        private Mock<IIdempotencyStore> _idempotencyStore;
        private Mock<ILogger<RefreshPaymentDataActivity>> _logger;

        private RefreshPaymentDataActivity _activity;

        private RefreshPaymentDataInput _input;

        [SetUp]
        public void Setup()
        {
            _providerApi = new Mock<IProviderPaymentApiClient<ProviderEventsApiConfiguration>>();
            _financeApi = new Mock<IFinanceApiClient<FinanceApiConfiguration>>();
            _idempotencyStore = new Mock<IIdempotencyStore>();
            _logger = new Mock<ILogger<RefreshPaymentDataActivity>>();

            _activity = new RefreshPaymentDataActivity(
                _providerApi.Object,
                _financeApi.Object,
                _idempotencyStore.Object,
                _logger.Object);

            _input = new RefreshPaymentDataInput
            {
                AccountId = 123,
                CorrelationId = "corr-1",
                IdempotencyKey = "key-1",
                PeriodEnd = new PeriodEnd { PeriodEndId = "2024-R12" }
            };
        }

        [Test]
        public async Task Run_ReturnsCachedResult_WhenIdempotencyStoreContainsValue()
        {
            var cached = new RefreshPaymentDataResult
            {
                CorrelationId = "corr-1",
                PaymentsCreated = 5
            };

            _idempotencyStore
                .Setup(x => x.GetAsync<RefreshPaymentDataResult>(_input.IdempotencyKey))
                .ReturnsAsync(cached);

            var result = await _activity.Run(_input);

            Assert.That(result, Is.EqualTo(cached));

            _providerApi.Verify(x =>
                x.GetWithResponseCode<List<Payment>>(It.IsAny<GetAccountPaymentsRequest>()),
                Times.Never);
        }

        [Test]
        public async Task Run_ReturnsEmptyResult_WhenProviderReturnsNoPayments()
        {
            _idempotencyStore
                .Setup(x => x.GetAsync<RefreshPaymentDataResult>(_input.IdempotencyKey))
                .ReturnsAsync((RefreshPaymentDataResult)null);

            _providerApi
                .Setup(x => x.GetWithResponseCode<List<Payment>>(It.IsAny<GetAccountPaymentsRequest>()))
                .ReturnsAsync(new ApiResponse<List<Payment>>(
                    new List<Payment>(),
                    HttpStatusCode.OK,
                    "",
                    new Dictionary<string, IEnumerable<string>>()));

            var result = await _activity.Run(_input);

            Assert.That(result.PaymentsCreated, Is.EqualTo(0));

            _idempotencyStore.Verify(x =>
                x.SaveAsync(_input.IdempotencyKey, It.IsAny<RefreshPaymentDataResult>()),
                Times.Once);
        }

        [Test]
        public async Task Run_FiltersExistingPayments()
        {
            var payments = new List<Payment>
            {
                new Payment { PaymentId = "1", FundingSource = "Levy" },
                new Payment { PaymentId = "2", FundingSource = "Levy" }
            };

            _idempotencyStore
                .Setup(x => x.GetAsync<RefreshPaymentDataResult>(_input.IdempotencyKey))
                .ReturnsAsync((RefreshPaymentDataResult)null);

            _providerApi
                .Setup(x => x.GetWithResponseCode<List<Payment>>(It.IsAny<GetAccountPaymentsRequest>()))
                .ReturnsAsync(new ApiResponse<List<Payment>>(
                    payments,
                    HttpStatusCode.OK,
                    "",
                    new Dictionary<string, IEnumerable<string>>()));

            _financeApi
                .Setup(x => x.GetWithResponseCode<List<string>>(It.IsAny<GetAccountPaymentIdsRequest>()))
                .ReturnsAsync(new ApiResponse<List<string>>(
                    new List<string> { "1" },
                    HttpStatusCode.OK,
                    "",
                    new Dictionary<string, IEnumerable<string>>()));

            var result = await _activity.Run(_input);

            Assert.That(result.PaymentsCreated, Is.EqualTo(1));

            _financeApi.Verify(x =>
                x.Post("/api/payments/staging", It.IsAny<object>()),
                Times.Once);
        }

        [Test]
        public async Task Run_IgnoresFullyFundedSfaPayments()
        {
            var payments = new List<Payment>
            {
                new Payment { PaymentId = "1", FundingSource = "FullyFundedSfa" },
                new Payment { PaymentId = "2", FundingSource = "Levy" }
            };

            _idempotencyStore
                .Setup(x => x.GetAsync<RefreshPaymentDataResult>(_input.IdempotencyKey))
                .ReturnsAsync((RefreshPaymentDataResult)null);

            _providerApi
                .Setup(x => x.GetWithResponseCode<List<Payment>>(It.IsAny<GetAccountPaymentsRequest>()))
                .ReturnsAsync(new ApiResponse<List<Payment>>(
                    payments,
                    HttpStatusCode.OK,
                    "",
                    new Dictionary<string, IEnumerable<string>>()));

            _financeApi
                .Setup(x => x.GetWithResponseCode<List<string>>(It.IsAny<GetAccountPaymentIdsRequest>()))
                .ReturnsAsync(new ApiResponse<List<string>>(
                    new List<string>(),
                    HttpStatusCode.OK,
                    "",
                    new Dictionary<string, IEnumerable<string>>()));

            var result = await _activity.Run(_input);

            Assert.That(result.PaymentsCreated, Is.EqualTo(1));
        }

        [Test]
        public async Task Run_PostsPaymentsInBatches()
        {
            var payments = Enumerable.Range(1, 5)
                .Select(i => new Payment
                {
                    PaymentId = i.ToString(),
                    FundingSource = "Levy"
                }).ToList();

            _idempotencyStore
                .Setup(x => x.GetAsync<RefreshPaymentDataResult>(_input.IdempotencyKey))
                .ReturnsAsync((RefreshPaymentDataResult)null);

            _providerApi
                .Setup(x => x.GetWithResponseCode<List<Payment>>(It.IsAny<GetAccountPaymentsRequest>()))
                .ReturnsAsync(new ApiResponse<List<Payment>>(
                    payments,
                    HttpStatusCode.OK,
                    "",
                    new Dictionary<string, IEnumerable<string>>()));

            _financeApi
                .Setup(x => x.GetWithResponseCode<List<string>>(It.IsAny<GetAccountPaymentIdsRequest>()))
                .ReturnsAsync(new ApiResponse<List<string>>(
                    new List<string>(),
                    HttpStatusCode.OK,
                    "",
                    new Dictionary<string, IEnumerable<string>>()));

            var result = await _activity.Run(_input);

            Assert.That(result.PaymentsCreated, Is.EqualTo(5));

            _financeApi.Verify(x =>
                x.Post("/api/payments/staging", It.IsAny<object>()),
                Times.AtLeastOnce);
        }
    }
}