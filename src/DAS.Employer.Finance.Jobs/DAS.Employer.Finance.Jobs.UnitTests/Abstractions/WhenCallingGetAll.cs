using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Collections.Generic;
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
public class WhenCallingGetAll
{
    private class TestBaseApiClient : BaseApiClient
    {
        public TestBaseApiClient(HttpClient httpClient, IAzureClientCredentialHelper credentialHelper, ILogger logger, IApiConfiguration configuration)
            : base(httpClient, credentialHelper, logger, configuration)
        {
        }
    }

    [Test, AutoData]
    public async Task Then_The_Endpoint_Is_Called(string authToken, TestApiConfiguration config)
    {
        //Arrange
        var azureClientCredentialHelper = new Mock<IAzureClientCredentialHelper>();
        azureClientCredentialHelper.Setup(x => x.GetAccessTokenAsync(config.Identifier)).ReturnsAsync(authToken);
        config.Url = "https://test.local";
        var responseList = new List<string> { "string1", "string2" };
        var response = new HttpResponseMessage
        {
            Content = new StringContent(JsonSerializer.Serialize(responseList), Encoding.UTF8, "application/json"),
            StatusCode = HttpStatusCode.OK
        };
        var getAllTestRequest = new GetAllTestRequest();
        var expectedUrl = $"{config.Url}/{getAllTestRequest.GetUrl}";
        var httpMessageHandler = MessageHandler.SetupMessageHandlerMock(response, expectedUrl);
        var client = new HttpClient(httpMessageHandler.Object);
        var mockLogger = new Mock<ILogger>();
        var apiClient = new TestBaseApiClient(client, azureClientCredentialHelper.Object, mockLogger.Object, config);

        //Act
        var actual = await apiClient.GetAll<string>(getAllTestRequest);

        //Assert
        actual.Should().NotBeNull();
        actual.Should().BeAssignableTo<IEnumerable<string>>();
        actual.Should().HaveCount(2);
        actual.Should().Contain("string1");
        actual.Should().Contain("string2");
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
    } 

    [Test, AutoData]
    public void Then_An_Exception_Is_Thrown_When_Response_Is_Not_Success(string authToken, TestApiConfiguration config)
    {
        //Arrange
        var azureClientCredentialHelper = new Mock<IAzureClientCredentialHelper>();
        azureClientCredentialHelper.Setup(x => x.GetAccessTokenAsync(config.Identifier)).ReturnsAsync(authToken);
        config.Url = "https://test.local";
        var response = new HttpResponseMessage
        {
            Content = new StringContent("Error", Encoding.UTF8, "text/plain"),
            StatusCode = HttpStatusCode.NotFound
        };
        var getAllTestRequest = new GetAllTestRequest();
        var expectedUrl = $"{config.Url}/{getAllTestRequest.GetUrl}";
        var httpMessageHandler = MessageHandler.SetupMessageHandlerMock(response, expectedUrl);
        var client = new HttpClient(httpMessageHandler.Object);
        var mockLogger = new Mock<ILogger>();
        var actual = new TestBaseApiClient(client, azureClientCredentialHelper.Object, mockLogger.Object, config);

        //Act & Assert
        Assert.ThrowsAsync<HttpRequestException>(() => actual.GetAll<string>(getAllTestRequest));
    }

    [Test, AutoData]
    public async Task Then_Empty_Collection_Is_Returned_When_Response_Is_Empty(string authToken, TestApiConfiguration config)
    {
        //Arrange
        var azureClientCredentialHelper = new Mock<IAzureClientCredentialHelper>();
        azureClientCredentialHelper.Setup(x => x.GetAccessTokenAsync(config.Identifier)).ReturnsAsync(authToken);
        config.Url = "https://test.local";
        var response = new HttpResponseMessage
        {
            Content = new StringContent("[]", Encoding.UTF8, "application/json"),
            StatusCode = HttpStatusCode.OK
        };
        var getAllTestRequest = new GetAllTestRequest();
        var expectedUrl = $"{config.Url}/{getAllTestRequest.GetUrl}";
        var httpMessageHandler = MessageHandler.SetupMessageHandlerMock(response, expectedUrl);
        var client = new HttpClient(httpMessageHandler.Object);
        var mockLogger = new Mock<ILogger>();
        var actual = new TestBaseApiClient(client, azureClientCredentialHelper.Object, mockLogger.Object, config);

        //Act
        var actualResult = await actual.GetAll<string>(getAllTestRequest);
        
        //Assert
        actualResult.Should().NotBeNull();
        actualResult.Should().BeEmpty();
    }        
    private class GetAllTestRequest : IGetAllApiRequest
    {
        public string GetUrl => "test-url/get-all";
    }
}