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
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Abstractions;

public class WhenCallingGetWithResponseCode
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
        var actualClient = new TestBaseApiClient(client, azureClientCredentialHelper.Object, mockLogger.Object, config);

        //Act
        var actual = await actualClient.GetWithResponseCode<string>(getTestRequest);

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
        actual.StatusCode.Should().Be(HttpStatusCode.OK);
        actual.Body.Should().Be("test");
    }   

    [Test, AutoData]
    public async Task Then_If_Returns_Not_Found_Result_Returns_Default_Body(int id, TestApiConfiguration config)
    {
        //Arrange
        config.Url = "https://test.local";
        var response = new HttpResponseMessage
        {
            Content = new StringContent("", Encoding.UTF8, "text/plain"),
            StatusCode = HttpStatusCode.NotFound
        };
        var getTestRequest = new GetTestRequestNoVersion(id);
        var expectedUrl = $"{config.Url}/{getTestRequest.GetUrl}";
        var httpMessageHandler = MessageHandler.SetupMessageHandlerMock(response, expectedUrl);
        var client = new HttpClient(httpMessageHandler.Object);
        var mockLogger = new Mock<ILogger>();
        var actual = new TestBaseApiClient(client, Mock.Of<IAzureClientCredentialHelper>(), mockLogger.Object, config);

        //Act
        var actualResult = await actual.GetWithResponseCode<string>(getTestRequest);

        //Assert
        actualResult.Should().NotBeNull();
        actualResult.StatusCode.Should().Be(HttpStatusCode.NotFound);
        actualResult.Body.Should().BeNull();
    }

    [Test, AutoData]
    public async Task Then_If_Returns_Error_Result_Returns_ErrorCode_With_Null_Body(string responseContent, int id, TestApiConfiguration config)
    {
        //Arrange
        config.Url = "https://test.local";
        config.Identifier = "";
        var response = new HttpResponseMessage
        {
            Content = new StringContent(responseContent, Encoding.UTF8, "text/plain"),
            StatusCode = HttpStatusCode.TooManyRequests
        };
        var getTestRequest = new GetTestRequestNoVersion(id);
        var expectedUrl = $"{config.Url}/{getTestRequest.GetUrl}";
        var httpMessageHandler = MessageHandler.SetupMessageHandlerMock(response, expectedUrl);
        var client = new HttpClient(httpMessageHandler.Object);
        var mockLogger = new Mock<ILogger>();
        var actual = new TestBaseApiClient(client, Mock.Of<IAzureClientCredentialHelper>(), mockLogger.Object, config);

        //Act
        var actualResult = await actual.GetWithResponseCode<string>(getTestRequest);

        //Assert
        actualResult.Should().NotBeNull();
        actualResult.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        actualResult.Body.Should().BeNull();
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

    private class GetTestRequestNoVersion : IGetApiRequest
    {
        private readonly int _id;

        public GetTestRequestNoVersion(int id)
        {
            _id = id;
        }
        public string GetUrl => $"test-url/get{_id}";
    }

  
}

