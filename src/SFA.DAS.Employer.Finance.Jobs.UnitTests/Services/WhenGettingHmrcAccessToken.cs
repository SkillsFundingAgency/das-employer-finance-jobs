using System;
using System.Net.Http;
using FluentAssertions;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;
using SFA.DAS.Employer.Finance.Jobs.UnitTests.Helpers;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Services;

public class WhenGettingHmrcAccessToken
{
    [Test]
    public async Task Then_The_Token_Is_Cached_Until_It_Expires()
    {
        var handler = new StubHttpMessageHandler(_ => StubHttpMessageHandler.JsonResponse("""
            {
              "access_token": "access-123",
              "expires_in": 3600
            }
            """));

        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(handler) { BaseAddress = new Uri("https://hmrc.test/") });

        var clock = new FakeHmrcClock(new DateTimeOffset(2026, 4, 13, 8, 0, 0, TimeSpan.Zero));
        var provider = new HmrcTokenProvider(
            httpClientFactory.Object,
            new HmrcConfiguration
            {
                BaseUrl = "https://hmrc.test/",
                ClientId = "client-id",
                ClientSecret = "client-secret",
                Scope = "read:apprenticeship-levy"
            },
            clock,
            new Mock<ILogger<HmrcTokenProvider>>().Object);

        var firstToken = await provider.GetAccessTokenAsync();
        var secondToken = await provider.GetAccessTokenAsync();

        firstToken.Should().Be("access-123");
        secondToken.Should().Be("access-123");
        handler.CallCount.Should().Be(1);
    }

    [Test]
    public void Then_A_Failed_Hmrc_Token_Response_Throws_A_Clear_Exception()
    {
        var handler = new StubHttpMessageHandler(_ => StubHttpMessageHandler.JsonResponse("""
            {
              "error": "invalid_client"
            }
            """, System.Net.HttpStatusCode.BadRequest));

        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(handler) { BaseAddress = new Uri("https://hmrc.test/") });

        var provider = new HmrcTokenProvider(
            httpClientFactory.Object,
            new HmrcConfiguration
            {
                BaseUrl = "https://hmrc.test/",
                ClientId = "client-id",
                ClientSecret = "client-secret",
                Scope = "read:apprenticeship-levy"
            },
            new FakeHmrcClock(new DateTimeOffset(2026, 4, 13, 8, 0, 0, TimeSpan.Zero)),
            new Mock<ILogger<HmrcTokenProvider>>().Object);

        var action = () => provider.GetAccessTokenAsync();

        action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Unable to retrieve an HMRC access token. Status code:*");
    }

    [Test]
    public void Then_A_Response_Without_An_Access_Token_Throws_A_Clear_Exception()
    {
        var handler = new StubHttpMessageHandler(_ => StubHttpMessageHandler.JsonResponse("""
            {
              "expires_in": 3600
            }
            """));

        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(handler) { BaseAddress = new Uri("https://hmrc.test/") });

        var provider = new HmrcTokenProvider(
            httpClientFactory.Object,
            new HmrcConfiguration
            {
                BaseUrl = "https://hmrc.test/",
                ClientId = "client-id",
                ClientSecret = "client-secret",
                Scope = "read:apprenticeship-levy"
            },
            new FakeHmrcClock(new DateTimeOffset(2026, 4, 13, 8, 0, 0, TimeSpan.Zero)),
            new Mock<ILogger<HmrcTokenProvider>>().Object);

        var action = () => provider.GetAccessTokenAsync();

        action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Unable to retrieve an HMRC access token because the response did not include an access token.");
    }
}
