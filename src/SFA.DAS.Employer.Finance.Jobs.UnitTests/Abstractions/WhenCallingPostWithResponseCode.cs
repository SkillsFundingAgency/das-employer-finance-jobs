using System.Net;
using System.Net.Http;
using System.Threading;
using AutoFixture.NUnit3;
using SFA.DAS.Api.Common.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Extensions;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Abstractions;

public class WhenCallingPostWithResponseCode
{
    [Test, AutoData]
    public async Task Then_Finance_Api_BadRequest_Returns_Error_Content_Instead_Of_Throwing(
        string authToken,
        string errorContent)
    {
        var config = new FinanceApiConfiguration
        {
            Url = "https://test.local",
            Identifier = "https://test.local/identifier"
        };
        var azureClientCredentialHelper = new Mock<IAzureClientCredentialHelper>();
        azureClientCredentialHelper.Setup(x => x.GetAccessTokenAsync(config.Identifier)).ReturnsAsync(authToken);

        var response = new HttpResponseMessage
        {
            Content = new StringContent(errorContent, System.Text.Encoding.UTF8, "application/json"),
            StatusCode = HttpStatusCode.BadRequest
        };

        var request = new PostTestRequest();
        var expectedUrl = $"{config.Url}/{request.GetUrl}";
        var httpMessageHandler = MessageHandler.SetupMessageHandlerMock(response, expectedUrl, "post");
        var client = new HttpClient(httpMessageHandler.Object);
        var clientFactory = new Mock<IHttpClientFactory>();
        clientFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);

        var apiClient = new InternalApiClient<FinanceApiConfiguration>(
            clientFactory.Object,
            config,
            azureClientCredentialHelper.Object);

        var actual = await apiClient.PostWithResponseCode<string>(request);

        actual.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        actual.ErrorContent.Should().Be(errorContent);
    }

    [Test, AutoData]
    public async Task Then_Non_Finance_Api_BadRequest_Throws_With_Error_Content(
        string authToken,
        string errorContent,
        TestInternalApiConfiguration config)
    {
        var azureClientCredentialHelper = new Mock<IAzureClientCredentialHelper>();
        azureClientCredentialHelper.Setup(x => x.GetAccessTokenAsync(config.Identifier)).ReturnsAsync(authToken);
        config.Url = "https://test.local";

        var response = new HttpResponseMessage
        {
            Content = new StringContent(errorContent, System.Text.Encoding.UTF8, "application/json"),
            StatusCode = HttpStatusCode.BadRequest
        };

        var request = new PostTestRequest();
        var expectedUrl = $"{config.Url}/{request.GetUrl}";
        var httpMessageHandler = MessageHandler.SetupMessageHandlerMock(response, expectedUrl, "post");
        var client = new HttpClient(httpMessageHandler.Object);
        var clientFactory = new Mock<IHttpClientFactory>();
        clientFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);

        var apiClient = new InternalApiClient<TestInternalApiConfiguration>(
            clientFactory.Object,
            config,
            azureClientCredentialHelper.Object);

        var act = () => apiClient.PostWithResponseCode<string>(request);

        var exception = await act.Should().ThrowAsync<HttpRequestContentException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        exception.Which.ErrorContent.Should().Be(errorContent);
        exception.Which.Message.Should().Contain(errorContent);
    }

    private class PostTestRequest : IApiRequest
    {
        public string GetUrl => "api/payments/staging";
        public object Data => new { Payments = Array.Empty<object>() };
    }
}
