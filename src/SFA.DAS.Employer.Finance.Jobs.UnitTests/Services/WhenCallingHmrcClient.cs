using HMRC.ESFA.Levy.Api.Client;
using HMRC.ESFA.Levy.Api.Types;
using HMRC.ESFA.Levy.Api.Types.Exceptions;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces.HMRC;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;
using SFA.DAS.Employer.Finance.Jobs.UnitTests.Helpers;
using System.Net.Http;
using System.Threading;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Services;

public class WhenCallingHmrcClient
{
    private Mock<IApprenticeshipLevyApiClient> _apprenticeshipLevyApiClient = null!;
    private Mock<IHmrcRateLimiter> _hmrcRateLimiter = null!;
    private Mock<IHmrcTokenProvider> _hmrcTokenProvider = null!;
    private FakeHmrcClock _hmrcClock = null!;
    private Mock<ILogger<HmrcClient>> _logger = null!;
    private HmrcClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _apprenticeshipLevyApiClient = new Mock<IApprenticeshipLevyApiClient>();
        _hmrcRateLimiter = new Mock<IHmrcRateLimiter>();
        _hmrcTokenProvider = new Mock<IHmrcTokenProvider>();
        _hmrcClock = new FakeHmrcClock(new DateTimeOffset(2026, 4, 13, 10, 0, 0, TimeSpan.Zero));
        _logger = new Mock<ILogger<HmrcClient>>();

        _hmrcRateLimiter.Setup(x => x.WaitForAvailabilityAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(TimeSpan.Zero);

        _hmrcTokenProvider.Setup(x => x.GetAccessTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("token-123");

        _client = new HmrcClient(
            _apprenticeshipLevyApiClient.Object,
            _hmrcRateLimiter.Object,
            _hmrcTokenProvider.Object,
            _hmrcClock,
            _logger.Object);
    }

    [Test]
    public async Task Then_429_Is_Retried_And_Succeeds()
    {
        var expectedDate = new DateTime(2026, 4, 1);

        _apprenticeshipLevyApiClient.SetupSequence(x => x.GetLastEnglishFractionUpdate("token-123"))
            .Throws(new ApiHttpException(429, "too many requests", string.Empty, string.Empty))
            .ReturnsAsync(expectedDate);

        var result = await _client.GetLastEnglishFractionUpdateAsync();

        result.Should().Be(expectedDate);
        _hmrcClock.Delays.Should().ContainSingle().Which.Should().Be(TimeSpan.FromSeconds(10));
        _apprenticeshipLevyApiClient.Verify(x => x.GetLastEnglishFractionUpdate("token-123"), Times.Exactly(2));
        _logger.VerifyLogContains(LogLevel.Warning, "Retrying");
    }

    [Test]
    public async Task Then_Transient_Http_Errors_Are_Retried_And_Succeed()
    {
        var expected = new EnglishFractionDeclarations
        {
            Empref = "123/AB456",
            FractionCalculations = []
        };

        _apprenticeshipLevyApiClient.SetupSequence(x => x.GetEmployerFractionCalculations("token-123", "123/AB456", null, null))
            .Throws(new HttpRequestException("temporary"))
            .ReturnsAsync(expected);

        var result = await _client.GetEnglishFractionsAsync("123/AB456", null);

        result.Should().BeSameAs(expected);
        _hmrcClock.Delays.Should().ContainSingle().Which.Should().Be(TimeSpan.FromSeconds(2));
    }

    [Test]
    public async Task Then_404_For_Fractions_Returns_An_Empty_Result()
    {
        _apprenticeshipLevyApiClient.Setup(x => x.GetEmployerFractionCalculations("token-123", "123/AB456", null, null))
            .Throws(new ApiHttpException(404, "not found", string.Empty, string.Empty));

        var result = await _client.GetEnglishFractionsAsync("123/AB456", null);

        result.Empref.Should().Be("123/AB456");
        result.FractionCalculations.Should().BeEmpty();
    }

    [Test]
    public async Task Then_404_For_Last_Update_Returns_Min_Value()
    {
        _apprenticeshipLevyApiClient.Setup(x => x.GetLastEnglishFractionUpdate("token-123"))
            .Throws(new ApiHttpException(404, "not found", string.Empty, string.Empty));

        var result = await _client.GetLastEnglishFractionUpdateAsync();

        result.Should().Be(DateTime.MinValue);
        _logger.VerifyLogContains(LogLevel.Information, "Returning an empty result");
    }

    [Test]
    public void Then_Non_Retryable_Errors_Are_Wrapped_With_Context()
    {
        _apprenticeshipLevyApiClient.Setup(x => x.GetLastEnglishFractionUpdate("token-123"))
            .Throws(new InvalidOperationException("boom"));

        var action = () => _client.GetLastEnglishFractionUpdateAsync();

        action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("HMRC call failed for GetLastEnglishFractionUpdate.");

        _logger.VerifyLogContains(LogLevel.Error, "after retries");
    }
}