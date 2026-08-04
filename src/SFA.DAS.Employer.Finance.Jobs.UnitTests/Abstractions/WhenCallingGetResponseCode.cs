using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using AutoFixture.NUnit4;
using Moq.Protected;
using SFA.DAS.Api.Common.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Extensions;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi;


namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Abstractions;
public class WhenCallingGetResponseCode
{

    [Test, AutoData]
    public async Task Then_The_Endpoint_Is_Called_And_StatusCode_Returned(string authToken,
            int id,
            TestInternalApiConfiguration config)
    {
        //Arrange
        var azureClientCredentialHelper = new Mock<IAzureClientCredentialHelper>();
        azureClientCredentialHelper.Setup(x => x.GetAccessTokenAsync(config.Identifier)).ReturnsAsync(authToken);
        config.Url = "https://test.local";
        var response = new HttpResponseMessage
        {
            Content = new StringContent("", System.Text.Encoding.UTF8, "application/json"),
            StatusCode = HttpStatusCode.OK
        };
        var getTestRequest = new GetTestRequest(id);
        var expectedUrl = $"{config.Url}/{getTestRequest.GetUrl}";
        var httpMessageHandler = MessageHandler.SetupMessageHandlerMock(response, expectedUrl);
        var client = new HttpClient(httpMessageHandler.Object);
        var clientFactory = new Mock<IHttpClientFactory>();
        clientFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);
        var actual = new InternalApiClient<TestInternalApiConfiguration>(clientFactory.Object, config, azureClientCredentialHelper.Object);

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
        actualResult.Should().Be(HttpStatusCode.OK);
    }

    [Test, AutoData]
    public async Task Then_NotFound_Throws(string authToken, int id, TestInternalApiConfiguration config)
    {
        var azureClientCredentialHelper = new Mock<IAzureClientCredentialHelper>();
        azureClientCredentialHelper.Setup(x => x.GetAccessTokenAsync(config.Identifier)).ReturnsAsync(authToken);
        config.Url = "https://test.local";
        var response = new HttpResponseMessage
        {
            Content = new StringContent("", System.Text.Encoding.UTF8, "application/json"),
            StatusCode = HttpStatusCode.NotFound
        };
        var getTestRequest = new GetTestRequest(id);
        var expectedUrl = $"{config.Url}/{getTestRequest.GetUrl}";
        var httpMessageHandler = MessageHandler.SetupMessageHandlerMock(response, expectedUrl);
        var client = new HttpClient(httpMessageHandler.Object);
        var clientFactory = new Mock<IHttpClientFactory>();
        clientFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);
        var actual = new InternalApiClient<TestInternalApiConfiguration>(clientFactory.Object, config, azureClientCredentialHelper.Object);

        var act = () => actual.GetResponseCode(getTestRequest);

        await act.Should().ThrowAsync<HttpRequestContentException>()
            .Where(ex => ex.StatusCode == HttpStatusCode.NotFound);
    }

    [Test, AutoData]
    public async Task Then_NonSuccess_Status_Codes_Throw(string authToken, int id, TestInternalApiConfiguration config)
    {
        var failingStatusCodes = new[]
        {
            HttpStatusCode.BadRequest,
            HttpStatusCode.InternalServerError,
            HttpStatusCode.Unauthorized
        };

        foreach (var statusCode in failingStatusCodes)
        {
            var azureClientCredentialHelper = new Mock<IAzureClientCredentialHelper>();
            azureClientCredentialHelper.Setup(x => x.GetAccessTokenAsync(config.Identifier)).ReturnsAsync(authToken);
            config.Url = "https://test.local";
            var response = new HttpResponseMessage
            {
                Content = new StringContent("", System.Text.Encoding.UTF8, "application/json"),
                StatusCode = statusCode
            };
            var getTestRequest = new GetTestRequest(id);
            var expectedUrl = $"{config.Url}/{getTestRequest.GetUrl}";
            var httpMessageHandler = MessageHandler.SetupMessageHandlerMock(response, expectedUrl);
            var client = new HttpClient(httpMessageHandler.Object);
            var clientFactory = new Mock<IHttpClientFactory>();
            clientFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);
            var actual = new InternalApiClient<TestInternalApiConfiguration>(clientFactory.Object, config, azureClientCredentialHelper.Object);

            var act = () => actual.GetResponseCode(getTestRequest);

            await act.Should().ThrowAsync<HttpRequestContentException>()
                .Where(ex => ex.StatusCode == statusCode);
        }
    }
    private class GetTestRequest : IApiRequest
    {
        private readonly int _id;

        public GetTestRequest(int id)
        {
            _id = id;
        }

        public string GetUrl => $"test-url/get{_id}";

        public object Data => throw new System.NotImplementedException();
    }
}
