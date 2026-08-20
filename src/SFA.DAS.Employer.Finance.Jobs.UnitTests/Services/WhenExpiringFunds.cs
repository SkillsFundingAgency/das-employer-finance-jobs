using System.Net;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Extensions;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.UnitTests.Helpers;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Services;

public class WhenExpiringFunds
{
    private Mock<IFinanceApiClient<FinanceApiConfiguration>> _financeApiClient = null!;
    private Mock<ILogger<ExpireFundsService>> _logger = null!;
    private ExpireFundsService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _financeApiClient = new Mock<IFinanceApiClient<FinanceApiConfiguration>>();
        _logger = new Mock<ILogger<ExpireFundsService>>();
        _service = new ExpireFundsService(_financeApiClient.Object, _logger.Object);
    }

    [Test]
    public async Task Then_The_Request_Uses_The_Account_Url_And_Passes_The_Correlation_Id()
    {
        IApiRequest capturedRequest = null!;
        _financeApiClient
            .Setup(client => client.PostWithResponseCode<ExpireFundsResponse>(It.IsAny<ExpireFundsRequest>()))
            .Callback<IApiRequest>(request => capturedRequest = request)
            .ReturnsAsync(SuccessResponse());

        await _service.ExpireFundsAsync(12345, "correlation-id");

        capturedRequest.Should().BeOfType<ExpireFundsRequest>();
        capturedRequest!.GetUrl.Should().Be("api/accounts/12345/expire-funds");
        capturedRequest.Data.Should().BeOfType<ExpireFundsRequestData>();
        ((ExpireFundsRequestData)capturedRequest.Data!).CorrelationId.Should().Be("correlation-id");
    }

    [Test]
    public async Task Then_A_Successful_Response_Is_Returned()
    {
        var expected = new ExpireFundsResponse
        {
            AccountId = 12345,
            CorrelationId = "correlation-id",
            FundsExpired = true,
            LongTermExpiredFundsCount = 2,
            ShortTermExpiredFundsCount = 1
        };
        _financeApiClient
            .Setup(client => client.PostWithResponseCode<ExpireFundsResponse>(It.IsAny<ExpireFundsRequest>()))
            .ReturnsAsync(new ApiResponse<ExpireFundsResponse>(expected, HttpStatusCode.OK, string.Empty));

        var result = await _service.ExpireFundsAsync(12345, "correlation-id");

        result.Should().BeSameAs(expected);
        _logger.VerifyLogContains(LogLevel.Information, "Funds expiry completed for AccountId 12345");
    }

    [Test]
    public async Task Then_A_NonSuccess_Response_Throws_With_The_Status_And_Response_Content()
    {
        _financeApiClient
            .Setup(client => client.PostWithResponseCode<ExpireFundsResponse>(It.IsAny<ExpireFundsRequest>()))
            .ReturnsAsync(new ApiResponse<ExpireFundsResponse>(
                null!,
                HttpStatusCode.Conflict,
                "Account cannot be processed"));

        var action = () => _service.ExpireFundsAsync(12345, "correlation-id");

        var exception = await action.Should().ThrowAsync<HttpRequestContentException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        exception.Which.ErrorContent.Should().Be("Account cannot be processed");
        _logger.VerifyLogContains(LogLevel.Error, "Employer Finance API failure");
    }

    [Test]
    public async Task Then_A_Transient_Error_Is_Logged_And_Rethrown_For_Retry()
    {
        _financeApiClient
            .Setup(client => client.PostWithResponseCode<ExpireFundsResponse>(It.IsAny<ExpireFundsRequest>()))
            .ThrowsAsync(new HttpRequestContentException(
                "Employer Finance API unavailable",
                HttpStatusCode.ServiceUnavailable,
                "Try again"));

        var action = () => _service.ExpireFundsAsync(12345, "correlation-id");

        var exception = await action.Should().ThrowAsync<HttpRequestContentException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        _logger.VerifyLogContains(LogLevel.Warning, "Transient Employer Finance API failure");
    }

    [Test]
    public async Task Then_A_Client_Exception_Is_Logged_And_Rethrown()
    {
        var expected = new InvalidOperationException("Client failure");
        _financeApiClient
            .Setup(client => client.PostWithResponseCode<ExpireFundsResponse>(It.IsAny<ExpireFundsRequest>()))
            .ThrowsAsync(expected);

        var action = () => _service.ExpireFundsAsync(12345, "correlation-id");

        var exception = await action.Should().ThrowAsync<InvalidOperationException>();
        exception.Which.Should().BeSameAs(expected);
        _logger.VerifyLogContains(LogLevel.Error, "Employer Finance API failure");
    }

    [Test]
    public async Task Then_A_Success_Response_Without_A_Body_Is_Rejected()
    {
        _financeApiClient
            .Setup(client => client.PostWithResponseCode<ExpireFundsResponse>(It.IsAny<ExpireFundsRequest>()))
            .ReturnsAsync(new ApiResponse<ExpireFundsResponse>(null!, HttpStatusCode.OK, string.Empty));

        var action = () => _service.ExpireFundsAsync(12345, "correlation-id");

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*without an expire-funds response body*");
    }

    private static ApiResponse<ExpireFundsResponse> SuccessResponse() =>
        new(
            new ExpireFundsResponse
            {
                AccountId = 12345,
                CorrelationId = "correlation-id"
            },
            HttpStatusCode.OK,
            string.Empty);
}
