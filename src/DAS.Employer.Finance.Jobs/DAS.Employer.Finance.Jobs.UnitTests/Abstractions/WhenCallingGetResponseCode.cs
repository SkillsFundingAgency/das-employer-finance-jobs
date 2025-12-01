using System.Net;
using System.Net.Http;
using System.Text;
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
public class WhenCallingGetResponseCode
{
    private class TestBaseApiClient : BaseApiClient
    {
        public TestBaseApiClient(HttpClient httpClient, IAzureClientCredentialHelper credentialHelper, ILogger logger, IApiConfiguration configuration)
            : base(httpClient, credentialHelper, logger, configuration)
        {
        }
    }

    [Test, AutoData]
    public async Task Then_The_Endpoint_Is_Called_And_StatusCode_Returned(string authToken, int id, HttpStatusCode code, TestApiConfiguration config)
    {
        //Arrange
        var azureClientCredentialHelper = new Mock<IAzureClientCredentialHelper>();
        azureClientCredentialHelper.Setup(x => x.GetAccessTokenAsync(config.Identifier)).ReturnsAsync(authToken);
        config.Url = "https://test.local";
        var response = new HttpResponseMessage
        {
            Content = new StringContent("", Encoding.UTF8, "application/json"),
            StatusCode = code
        };
        var getTestRequest = new GetTestRequest(id);
        var expectedUrl = $"{config.Url}/{getTestRequest.GetUrl}";
        var httpMessageHandler = MessageHandler.SetupMessageHandlerMock(response, expectedUrl);
        var client = new HttpClient(httpMessageHandler.Object);
        var mockLogger = new Mock<ILogger>();
        var actual = new TestBaseApiClient(client, azureClientCredentialHelper.Object, mockLogger.Object, config);

        //Act
        var actualResult = await actual.GetResponseCode(getTestRequest);

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
        actualResult.Should().Be(code);
    }  

    [Test, AutoData]
    public async Task Then_All_Status_Codes_Are_Returned_Correctly(string authToken, int id, TestApiConfiguration config)
    {
        //Arrange
        var statusCodes = new[]
        {
            HttpStatusCode.OK,
            HttpStatusCode.NotFound,
            HttpStatusCode.BadRequest,
            HttpStatusCode.InternalServerError,
            HttpStatusCode.Unauthorized
        };

        foreach (var statusCode in statusCodes)
        {
            var azureClientCredentialHelper = new Mock<IAzureClientCredentialHelper>();
            azureClientCredentialHelper.Setup(x => x.GetAccessTokenAsync(config.Identifier)).ReturnsAsync(authToken);
            config.Url = "https://test.local";
            var response = new HttpResponseMessage
            {
                Content = new StringContent("", Encoding.UTF8, "application/json"),
                StatusCode = statusCode
            };
            var getTestRequest = new GetTestRequest(id);
            var expectedUrl = $"{config.Url}/{getTestRequest.GetUrl}";
            var httpMessageHandler = MessageHandler.SetupMessageHandlerMock(response, expectedUrl);
            var client = new HttpClient(httpMessageHandler.Object);
            var mockLogger = new Mock<ILogger>();
            var actual = new TestBaseApiClient(client, azureClientCredentialHelper.Object, mockLogger.Object, config);

            //Act
            var actualResult = await actual.GetResponseCode(getTestRequest);

            //Assert
            actualResult.Should().Be(statusCode);
        }
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
}