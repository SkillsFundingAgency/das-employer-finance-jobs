using System.Net;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Services;

[TestFixture]
public class WhenGettingRoatpProvider
{
    private Mock<IInternalApiClient<RoatpApiConfiguration>> _apiClientMock;
    private Mock<ILogger<RoatpApiClient>> _loggerMock;
    private RoatpApiClient _client;

    [SetUp]
    public void SetUp()
    {
        _apiClientMock = new Mock<IInternalApiClient<RoatpApiConfiguration>>();
        _loggerMock = new Mock<ILogger<RoatpApiClient>>();
        _client = new RoatpApiClient(_apiClientMock.Object, _loggerMock.Object);
    }

    [Test]
    public async Task Then_Returns_Provider_Details_When_Roatp_Responds()
    {
        _apiClientMock
            .Setup(client => client.Get<RoatpProviderApiResponse>(It.IsAny<GetRoatpProviderRequest>()))
            .ReturnsAsync(new RoatpProviderApiResponse { Ukprn = 10007822, Name = "Test Provider" });

        var result = await _client.GetProvider(10007822);

        result.Should().NotBeNull();
        result!.Ukprn.Should().Be(10007822);
        result.Name.Should().Be("Test Provider");
    }

    [Test]
    public async Task Then_Returns_Null_When_The_Provider_Is_Missing()
    {
        _apiClientMock
            .Setup(client => client.Get<RoatpProviderApiResponse>(It.IsAny<GetRoatpProviderRequest>()))
            .ReturnsAsync((RoatpProviderApiResponse)null);

        var result = await _client.GetProvider(10007822);

        result.Should().BeNull();
    }

    [Test]
    public async Task Then_Does_Not_Treat_Authentication_Failure_As_A_Missing_Provider()
    {
        _apiClientMock
            .Setup(client => client.Get<RoatpProviderApiResponse>(It.IsAny<GetRoatpProviderRequest>()))
            .ThrowsAsync(new AuthenticationFailedException("IMDS returned 400"));

        var act = () => _client.GetProvider(10007822);

        await act.Should().ThrowAsync<AuthenticationFailedException>();
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("RoATP authentication failed")),
                It.IsAny<AuthenticationFailedException>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("Unable to get provider details")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Never);
    }
}
