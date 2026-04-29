using FluentAssertions;
using HMRC.ESFA.Levy.Api.Client;
using HMRC.ESFA.Levy.Api.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using SFA.DAS.ActiveDirectory;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.UnitTests.Helpers;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Exceptions;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces.HMRC;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services.HMRC;
using SFA.DAS.TokenService.Api.Client;
using System.Net;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.UnitTests.Services.HMRC;

[TestFixture]
public class HmrcServiceTests
{
    private const string AccessToken = "access-token";
    private const string CorrelationId = "corr-123";
    private const string EmpRef = "123/AB12345";

    [Test]
    public async Task GetLevyDeclarations_Passes_Provided_FromDate_To_Hmrc_Client()
    {
        var fromDate = new DateTime(2026, 1, 1);
        var expectedDeclarations = CreateDeclarations();
        var hmrcClient = new Mock<IApprenticeshipLevyApiClient>();
        hmrcClient
            .Setup(x => x.GetEmployerLevyDeclarations(AccessToken, EmpRef, fromDate, It.IsAny<DateTime?>()))
            .ReturnsAsync(expectedDeclarations);

        var service = CreateService(hmrcClient: hmrcClient);

        var result = await service.GetLevyDeclarations(EmpRef, fromDate, CorrelationId, CancellationToken.None);

        result.Should().BeSameAs(expectedDeclarations);
        hmrcClient.Verify(
            x => x.GetEmployerLevyDeclarations(AccessToken, EmpRef, fromDate, It.IsAny<DateTime?>()),
            Times.Once);
    }

    [Test]
    public async Task GetLevyDeclarations_Uses_Earliest_Date_When_FromDate_Is_Null()
    {
        var earliestDate = new DateTime(2017, 4, 1);
        var hmrcClient = new Mock<IApprenticeshipLevyApiClient>();
        hmrcClient
            .Setup(x => x.GetEmployerLevyDeclarations(AccessToken, EmpRef, earliestDate, It.IsAny<DateTime?>()))
            .ReturnsAsync(CreateDeclarations());

        var service = CreateService(hmrcClient: hmrcClient);

        await service.GetLevyDeclarations(EmpRef, null, CorrelationId, CancellationToken.None);

        hmrcClient.Verify(
            x => x.GetEmployerLevyDeclarations(AccessToken, EmpRef, earliestDate, It.IsAny<DateTime?>()),
            Times.Once);
    }

    [Test]
    public async Task GetLevyDeclarations_Clamps_FromDate_To_Earliest_Date_When_Provided_Date_Is_Earlier()
    {
        var earliestDate = new DateTime(2017, 4, 1);
        var hmrcClient = new Mock<IApprenticeshipLevyApiClient>();
        hmrcClient
            .Setup(x => x.GetEmployerLevyDeclarations(AccessToken, EmpRef, earliestDate, It.IsAny<DateTime?>()))
            .ReturnsAsync(CreateDeclarations());

        var service = CreateService(hmrcClient: hmrcClient);

        await service.GetLevyDeclarations(EmpRef, new DateTime(2016, 12, 31), CorrelationId, CancellationToken.None);

        hmrcClient.Verify(
            x => x.GetEmployerLevyDeclarations(AccessToken, EmpRef, earliestDate, It.IsAny<DateTime?>()),
            Times.Once);
    }

    [Test]
    public async Task GetLevyDeclarations_Coalesces_Null_Hmrc_Response_To_Empty_Declarations()
    {
        var hmrcClient = new Mock<IApprenticeshipLevyApiClient>();
        hmrcClient
            .Setup(x => x.GetEmployerLevyDeclarations(AccessToken, EmpRef, It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync((LevyDeclarations?)null);

        var service = CreateService(hmrcClient: hmrcClient);

        var result = await service.GetLevyDeclarations(EmpRef, new DateTime(2026, 1, 1), CorrelationId, CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Test]
    public async Task GetLevyDeclarations_Calls_RateLimiter_Before_Each_Hmrc_Attempt()
    {
        var fromDate = new DateTime(2026, 1, 1);
        var hmrcClient = new Mock<IApprenticeshipLevyApiClient>();
        hmrcClient
            .SetupSequence(x => x.GetEmployerLevyDeclarations(AccessToken, EmpRef, fromDate, It.IsAny<DateTime?>()))
            .ThrowsAsync(new HmrcApiException(HttpStatusCode.TooManyRequests, "Too many requests"))
            .ReturnsAsync(CreateDeclarations());

        var rateLimiter = new Mock<IHmrcRateLimiter>();
        rateLimiter
            .Setup(x => x.WaitForAvailabilityAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(TimeSpan.Zero);

        var service = CreateService(
            hmrcClient: hmrcClient,
            rateLimiter: rateLimiter,
            options: CreateOptions(maxRetries: 1));

        await service.GetLevyDeclarations(EmpRef, fromDate, CorrelationId, CancellationToken.None);

        rateLimiter.Verify(
            x => x.WaitForAvailabilityAsync(It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Test]
    public async Task GetLevyDeclarations_Retries_TooManyRequests_With_Backoff()
    {
        var fromDate = new DateTime(2026, 1, 1);
        var hmrcClient = new Mock<IApprenticeshipLevyApiClient>();
        hmrcClient
            .SetupSequence(x => x.GetEmployerLevyDeclarations(AccessToken, EmpRef, fromDate, It.IsAny<DateTime?>()))
            .ThrowsAsync(new HmrcApiException(HttpStatusCode.TooManyRequests, "Too many requests"))
            .ReturnsAsync(CreateDeclarations());

        var service = CreateService(
            hmrcClient: hmrcClient,
            options: CreateOptions(maxRetries: 1));

        await service.GetLevyDeclarations(EmpRef, fromDate, CorrelationId, CancellationToken.None);

        hmrcClient.Verify(
            x => x.GetEmployerLevyDeclarations(AccessToken, EmpRef, fromDate, It.IsAny<DateTime?>()),
            Times.Exactly(2));
    }

    [TestCase(HttpStatusCode.InternalServerError)]
    [TestCase(HttpStatusCode.BadGateway)]
    [TestCase(HttpStatusCode.ServiceUnavailable)]
    [TestCase(HttpStatusCode.GatewayTimeout)]
    public async Task GetLevyDeclarations_Retries_Transient_Hmrc_StatusCodes(HttpStatusCode statusCode)
    {
        var fromDate = new DateTime(2026, 1, 1);
        var hmrcClient = new Mock<IApprenticeshipLevyApiClient>();
        hmrcClient
            .SetupSequence(x => x.GetEmployerLevyDeclarations(AccessToken, EmpRef, fromDate, It.IsAny<DateTime?>()))
            .ThrowsAsync(new HmrcApiException(statusCode, "Transient failure"))
            .ReturnsAsync(CreateDeclarations());

        var service = CreateService(
            hmrcClient: hmrcClient,
            options: CreateOptions(maxRetries: 1));

        await service.GetLevyDeclarations(EmpRef, fromDate, CorrelationId, CancellationToken.None);

        hmrcClient.Verify(
            x => x.GetEmployerLevyDeclarations(AccessToken, EmpRef, fromDate, It.IsAny<DateTime?>()),
            Times.Exactly(2));
    }

    [TestCase(typeof(HttpRequestException))]
    [TestCase(typeof(TaskCanceledException))]
    public async Task GetLevyDeclarations_Retries_Transient_Exceptions(Type exceptionType)
    {
        var fromDate = new DateTime(2026, 1, 1);
        var exception = (Exception)Activator.CreateInstance(exceptionType, "Transient failure")!;
        var hmrcClient = new Mock<IApprenticeshipLevyApiClient>();
        hmrcClient
            .SetupSequence(x => x.GetEmployerLevyDeclarations(AccessToken, EmpRef, fromDate, It.IsAny<DateTime?>()))
            .ThrowsAsync(exception)
            .ReturnsAsync(CreateDeclarations());

        var service = CreateService(
            hmrcClient: hmrcClient,
            options: CreateOptions(maxRetries: 1));

        await service.GetLevyDeclarations(EmpRef, fromDate, CorrelationId, CancellationToken.None);

        hmrcClient.Verify(
            x => x.GetEmployerLevyDeclarations(AccessToken, EmpRef, fromDate, It.IsAny<DateTime?>()),
            Times.Exactly(2));
    }

    [Test]
    public async Task GetLevyDeclarations_Does_Not_Retry_NonTransient_Hmrc_StatusCode()
    {
        var fromDate = new DateTime(2026, 1, 1);
        var hmrcClient = new Mock<IApprenticeshipLevyApiClient>();
        hmrcClient
            .Setup(x => x.GetEmployerLevyDeclarations(AccessToken, EmpRef, fromDate, It.IsAny<DateTime?>()))
            .ThrowsAsync(new HmrcApiException(HttpStatusCode.NotFound, "Not found"));

        var service = CreateService(
            hmrcClient: hmrcClient,
            options: CreateOptions(maxRetries: 3));

        Func<Task> act = async () => await service.GetLevyDeclarations(EmpRef, fromDate, CorrelationId, CancellationToken.None);

        await act.Should().ThrowAsync<HmrcApiException>();
        hmrcClient.Verify(
            x => x.GetEmployerLevyDeclarations(AccessToken, EmpRef, fromDate, It.IsAny<DateTime?>()),
            Times.Once);
    }

    [Test]
    public async Task GetLevyDeclarations_Logs_When_Request_Is_Delayed_By_RateLimiter()
    {
        var rateLimiter = new Mock<IHmrcRateLimiter>();
        rateLimiter
            .Setup(x => x.WaitForAvailabilityAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(TimeSpan.FromMilliseconds(25));

        var logger = new Mock<ILogger<HmrcService>>();
        var service = CreateService(rateLimiter: rateLimiter, logger: logger);

        await service.GetLevyDeclarations(EmpRef, new DateTime(2026, 1, 1), CorrelationId, CancellationToken.None);

        logger.VerifyLogContains("HMRC levy request delayed due to throttling");
    }

    [Test]
    public void LevyImportResilienceOptions_Defaults_Match_Acceptance_Criteria()
    {
        var options = new LevyImportResilienceOptions();

        options.MaxRetries.Should().Be(4);
        options.BaseDelayMilliseconds.Should().Be(500);
        options.JitterMilliseconds.Should().Be(250);
        options.MaxRequestsPerWindow.Should().Be(6);
        options.WindowSeconds.Should().Be(2);
    }

    private static HmrcService CreateService(
        Mock<IApprenticeshipLevyApiClient>? hmrcClient = null,
        Mock<IHmrcRateLimiter>? rateLimiter = null,
        LevyImportResilienceOptions? options = null,
        Mock<ILogger<HmrcService>>? logger = null)
    {
        var useDefaultHmrcClientSetup = hmrcClient == null;
        var useDefaultRateLimiterSetup = rateLimiter == null;
        var configuration = new HmrcConfiguration
        {
            UseHiDataFeed = true,
            ClientId = "client-id",
            AzureAppKey = "app-key",
            AzureResourceId = "resource-id",
            AzureTenant = "tenant"
        };

        var azureAdAuthenticationService = new Mock<IAzureAdAuthenticationService>();
        azureAdAuthenticationService
            .Setup(x => x.GetAuthenticationResult(
                configuration.ClientId,
                configuration.AzureAppKey,
                configuration.AzureResourceId,
                configuration.AzureTenant))
            .ReturnsAsync(AccessToken);

        hmrcClient ??= new Mock<IApprenticeshipLevyApiClient>();
        if (useDefaultHmrcClientSetup)
        {
            hmrcClient
                .Setup(x => x.GetEmployerLevyDeclarations(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
                .ReturnsAsync(CreateDeclarations());
        }

        rateLimiter ??= new Mock<IHmrcRateLimiter>();
        if (useDefaultRateLimiterSetup)
        {
            rateLimiter
                .Setup(x => x.WaitForAvailabilityAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(TimeSpan.Zero);
        }

        return new HmrcService(
            configuration,
            hmrcClient.Object,
            Mock.Of<ITokenServiceApiClient>(),
            azureAdAuthenticationService.Object,
            Options.Create(options ?? CreateOptions()),
            rateLimiter.Object,
            logger?.Object ?? Mock.Of<ILogger<HmrcService>>());
    }

    private static LevyImportResilienceOptions CreateOptions(int maxRetries = 4)
    {
        return new LevyImportResilienceOptions
        {
            MaxRetries = maxRetries,
            BaseDelayMilliseconds = 1,
            JitterMilliseconds = 0,
            MaxRequestsPerWindow = 6,
            WindowSeconds = 2
        };
    }

    private static LevyDeclarations CreateDeclarations()
    {
        return new LevyDeclarations
        {
            Declarations = []
        };
    }
}
