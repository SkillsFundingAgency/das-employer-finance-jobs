using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Functions.Activities;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;
using System.Collections.Generic;
using System.Net;

namespace SFA.DAS.Employer.Finance.Jobs.Tests.Functions.Activities
{
    [TestFixture]
    public class GetLevyAccountsActivityTests
    {
        private Mock<IFinanceApiClient<FinanceApiConfiguration>> _financeApiMock;
        private Mock<ILogger<GetLevyAccountsActivity>> _loggerMock;

        private GetLevyAccountsActivity _activity;

        [SetUp]
        public void Setup()
        {
            _financeApiMock = new Mock<IFinanceApiClient<FinanceApiConfiguration>>();
            _loggerMock = new Mock<ILogger<GetLevyAccountsActivity>>();

            _activity = new GetLevyAccountsActivity(
                _financeApiMock.Object,
                _loggerMock.Object);
        }

        [Test]
        public async Task Run_ReturnsAccounts_WhenApiReturnsOk()
        {
            // Arrange
            var accounts = new List<long> { 1, 2, 3 };

            var apiResponse = new ApiResponse<List<long>>(
                accounts,
                HttpStatusCode.OK,
                string.Empty,
                new Dictionary<string, IEnumerable<string>>()
            );

            _financeApiMock
                .Setup(x => x.GetWithResponseCode<List<long>>(It.IsAny<GetLevyAccountsRequest>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _activity.Run("corr-123");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(result, Is.EquivalentTo(accounts));
        }

        [Test]
        public async Task Run_ReturnsEmptyList_WhenStatusCodeNotOk()
        {
            // Arrange
            var apiResponse = new ApiResponse<List<long>>(
                null,
                HttpStatusCode.InternalServerError,
                "error",
                new Dictionary<string, IEnumerable<string>>()
            );

            _financeApiMock
                .Setup(x => x.GetWithResponseCode<List<long>>(It.IsAny<GetLevyAccountsRequest>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _activity.Run("corr-123");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task Run_ReturnsEmptyList_WhenBodyIsNull()
        {
            // Arrange
            var apiResponse = new ApiResponse<List<long>>(
                null,
                HttpStatusCode.OK,
                string.Empty,
                new Dictionary<string, IEnumerable<string>>()
            );

            _financeApiMock
                .Setup(x => x.GetWithResponseCode<List<long>>(It.IsAny<GetLevyAccountsRequest>()))
                .ReturnsAsync(apiResponse);

            // Act
            var result = await _activity.Run("corr-123");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void Run_ThrowsException_WhenApiFails()
        {
            // Arrange
            _financeApiMock
                .Setup(x => x.GetWithResponseCode<List<long>>(It.IsAny<GetLevyAccountsRequest>()))
                .ThrowsAsync(new Exception("API failure"));

            // Act / Assert
            Assert.ThrowsAsync<Exception>(() => _activity.Run("corr-123"));
        }
    }
}