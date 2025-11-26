using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SFA.DAS.Api.Common.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Abstractions;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Abstractions;

public class WhenUsingBaseApiClient
{
    private const string BaseUrl = "https://test.local/";

    private class TestApiConfiguration : IApiConfiguration
    {
        public string Url { get; set; } = BaseUrl;
        public string Identifier { get; set; } = "test-id";
    }
    private class FakeGetRequest : IGetApiRequest
    {
        public string GetUrl { get; }
        public FakeGetRequest(string url) => GetUrl = url;
    }
     
    private class FakeGetAllRequest : IGetAllApiRequest
    {
        public string GetUrl { get; }
        public FakeGetAllRequest(string url) => GetUrl = url;
    }
      
    private class TestBaseApiClient : BaseApiClient
    {
        public TestBaseApiClient(HttpClient httpClient, IAzureClientCredentialHelper credentialHelper, ILogger logger, IApiConfiguration configuration)
            : base(httpClient, credentialHelper, logger, configuration)
        {
        }

        public HttpClient ExposedHttpClient => HttpClient;
    }
       
    private class DelegatingHandlerStub : DelegatingHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public DelegatingHandlerStub(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder ?? throw new ArgumentNullException(nameof(responder));
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_responder(request));
        }
    }

    [Test]
    public async Task Get_Should_deserialize_and_set_authorization_header()
    {
        // Arrange
        var token = "token-123";
        var payload = new Dictionary<string, string> { ["value"] = "ok" };
        var json = JsonSerializer.Serialize(payload);

        var handler = new DelegatingHandlerStub(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };

        var mockCredentialHelper = new Mock<IAzureClientCredentialHelper>();
        mockCredentialHelper.Setup(x => x.GetAccessTokenAsync(It.IsAny<string>())).ReturnsAsync(token);

        var mockLogger = new Mock<ILogger>();

        var client = new TestBaseApiClient(httpClient, mockCredentialHelper.Object, mockLogger.Object, new TestApiConfiguration());

        // Act
        var result = await client.Get<Dictionary<string, string>>(new FakeGetRequest("some/path"));

        // Assert
        result.Should().NotBeNull();
        result.Should().ContainKey("value").WhoseValue.Should().Be("ok");
        client.ExposedHttpClient.DefaultRequestHeaders.Authorization.Should().NotBeNull();
        client.ExposedHttpClient.DefaultRequestHeaders.Authorization!.Scheme.Should().Be("Bearer");
        client.ExposedHttpClient.DefaultRequestHeaders.Authorization!.Parameter.Should().Be(token);
    }
      

    [Test]
    public async Task GetResponseCode_Should_return_status_code()
    {
        // Arrange
        var handler = new DelegatingHandlerStub(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };

        var mockCredentialHelper = new Mock<IAzureClientCredentialHelper>();
        mockCredentialHelper.Setup(x => x.GetAccessTokenAsync(It.IsAny<string>())).ReturnsAsync("t");

        var mockLogger = new Mock<ILogger>();

        var client = new TestBaseApiClient(httpClient, mockCredentialHelper.Object, mockLogger.Object, new TestApiConfiguration());

        // Act
        var status = await client.GetResponseCode(new FakeGetRequest("x"));

        // Assert
        status.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetWithResponseCode_Should_return_body_when_success()
    {
        // Arrange
        var obj = new { Body = "Api Client Testing" };
        var json = JsonSerializer.Serialize(obj);

        var handler = new DelegatingHandlerStub(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };

        var mockCredentialHelper = new Mock<IAzureClientCredentialHelper>();
        mockCredentialHelper.Setup(x => x.GetAccessTokenAsync(It.IsAny<string>())).ReturnsAsync("t");

        var mockLogger = new Mock<ILogger>();

        var client = new TestBaseApiClient(httpClient, mockCredentialHelper.Object, mockLogger.Object, new TestApiConfiguration());

        // Act
        var result = await client.GetWithResponseCode<Dictionary<string, string>>(new FakeGetRequest("y"));

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Body.Should().NotBeNull();
        result.Body.Should().ContainKey("Body").WhoseValue.Should().Be("Api Client Testing");
    }

    [Test]
    public async Task GetWithResponseCode_Should_return_default_body_when_not_success()
    {
        // Arrange
        var handler = new DelegatingHandlerStub(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("server error", Encoding.UTF8, "text/plain")
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };

        var mockCredentialHelper = new Mock<IAzureClientCredentialHelper>();
        mockCredentialHelper.Setup(x => x.GetAccessTokenAsync(It.IsAny<string>())).ReturnsAsync("t");

        var mockLogger = new Mock<ILogger>();

        var client = new TestBaseApiClient(httpClient, mockCredentialHelper.Object, mockLogger.Object, new TestApiConfiguration());

        // Act
        var result = await client.GetWithResponseCode<Dictionary<string, string>>(new FakeGetRequest("z"));

        // Assert
        result.Should().NotBeNull();
        result.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        result.Body.Should().BeNull();
    }
}