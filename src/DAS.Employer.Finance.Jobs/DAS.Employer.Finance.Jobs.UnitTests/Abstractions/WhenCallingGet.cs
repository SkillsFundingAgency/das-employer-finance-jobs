using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.NUnit3;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using NUnit.Framework;
using SFA.DAS.Api.Common.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Abstractions;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Abstractions;

public class WhenCallingGet
{
    private class TestBaseApiClient : BaseApiClient
    {
        public TestBaseApiClient(HttpClient httpClient, IAzureClientCredentialHelper credentialHelper, ILogger logger, IApiConfiguration configuration)
            : base(httpClient, credentialHelper, logger, configuration)
        {
        }
    }

    [Test, AutoData]
    public async Task Then_The_Endpoint_Is_Called(string authToken, int id, TestApiConfiguration config)
    {
        //Arrange
        var azureClientCredentialHelper = new Mock<IAzureClientCredentialHelper>();
        azureClientCredentialHelper.Setup(x => x.GetAccessTokenAsync(config.Identifier)).ReturnsAsync(authToken);
        config.Url = "https://test.local";
        var response = new HttpResponseMessage
        {
            Content = new StringContent("\"test\"", Encoding.UTF8, "application/json"),
            StatusCode = HttpStatusCode.OK
        };
        var getTestRequest = new GetTestRequest(id);
        var expectedUrl = $"{config.Url}/{getTestRequest.GetUrl}";
        var httpMessageHandler = MessageHandler.SetupMessageHandlerMock(response, expectedUrl);
        var client = new HttpClient(httpMessageHandler.Object);
        var mockLogger = new Mock<ILogger>();
        var actual = new TestBaseApiClient(client, azureClientCredentialHelper.Object, mockLogger.Object, config);

        //Act
        var result = await actual.Get<string>(getTestRequest);

        //Assert
        httpMessageHandler.Protected()
            .Verify<Task<HttpResponseMessage>>(
                "SendAsync", Times.Once(),
                ItExpr.Is<HttpRequestMessage>(c =>
                    c.Method.Equals(HttpMethod.Get)
                    && c.RequestUri.AbsoluteUri.Equals(expectedUrl)
                    && c.Headers.Authorization.Scheme.Equals("Bearer")
                    && c.Headers.Authorization.Parameter.Equals(authToken)),
                ItExpr.IsAny<CancellationToken>()
            );
        result.Should().Be("test");
    }    

    [Test, AutoData]
    public async Task Then_The_Response_Is_Correctly_Deserialized(string authToken, int id, string responseValue, TestApiConfiguration config)
    {
        //Arrange
        var azureClientCredentialHelper = new Mock<IAzureClientCredentialHelper>();
        azureClientCredentialHelper.Setup(x => x.GetAccessTokenAsync(config.Identifier)).ReturnsAsync(authToken);
        config.Url = "https://test.local";
        var testObject = JsonSerializer.Serialize(new TestResponse { MyResponse = responseValue });
        var response = new HttpResponseMessage
        {
            Content = new StringContent(testObject, Encoding.UTF8, "application/json"),
            StatusCode = HttpStatusCode.OK
        };
        var getTestRequest = new GetTestRequest(id);
        var expectedUrl = $"{config.Url}/{getTestRequest.GetUrl}";
        var httpMessageHandler = MessageHandler.SetupMessageHandlerMock(response, expectedUrl);
        var client = new HttpClient(httpMessageHandler.Object);
        var mockLogger = new Mock<ILogger>();
        var actual = new TestBaseApiClient(client, azureClientCredentialHelper.Object, mockLogger.Object, config);

        //Act
        var result = await actual.Get<TestResponse>(getTestRequest);

        //Assert
        result.Should().NotBeNull();
        result.MyResponse.Should().Be(responseValue);
    }

    [Test, AutoData]
    public void Then_An_Exception_Is_Thrown_When_Response_Is_Not_Success(string authToken, int id, TestApiConfiguration config)
    {
        //Arrange
        var azureClientCredentialHelper = new Mock<IAzureClientCredentialHelper>();
        azureClientCredentialHelper.Setup(x => x.GetAccessTokenAsync(config.Identifier)).ReturnsAsync(authToken);
        config.Url = "https://test.local";
        var response = new HttpResponseMessage
        {
            Content = new StringContent("Error", Encoding.UTF8, "text/plain"),
            StatusCode = HttpStatusCode.InternalServerError
        };
        var getTestRequest = new GetTestRequest(id);
        var expectedUrl = $"{config.Url}/{getTestRequest.GetUrl}";
        var httpMessageHandler = MessageHandler.SetupMessageHandlerMock(response, expectedUrl);
        var client = new HttpClient(httpMessageHandler.Object);
        var mockLogger = new Mock<ILogger>();
        var actual = new TestBaseApiClient(client, azureClientCredentialHelper.Object, mockLogger.Object, config);

        //Act & Assert
        Assert.ThrowsAsync<HttpRequestException>(() => actual.Get<string>(getTestRequest));
    }
   

    private class GetTestRequest : IGetApiRequest
    {
        private readonly int _id;

        public GetTestRequest(int id)
        {
            _id = id;
        }
        public string GetUrl => $"test-url/get{_id}";
    }

    private class TestResponse
    {
        public string MyResponse { get; set; }
    }
}

