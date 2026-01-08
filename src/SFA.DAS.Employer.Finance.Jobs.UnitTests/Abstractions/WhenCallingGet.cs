//using System.Linq;
//using System.Net;
//using System.Net.Http;
//using System.Text;
//using System.Text.Json;
//using System.Threading;
//using System.Threading.Tasks;
//using AutoFixture.NUnit3;
//using FluentAssertions;
//using Moq;
//using Moq.Protected;
//using NUnit.Framework;
//using SFA.DAS.Api.Common.Interfaces;
//using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
//using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi;

//namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Abstractions;
//public class WhenCallingGet
//{ 

//    [Test, AutoData]
//    public async Task Then_The_Endpoint_Is_Called(
//            string authToken,
//            int id,
//            TestInternalApiConfiguration config)
//    {
//        //Arrange
//        var azureClientCredentialHelper = new Mock<IAzureClientCredentialHelper>();
//        azureClientCredentialHelper.Setup(x => x.GetAccessTokenAsync(config.Identifier)).ReturnsAsync(authToken);
//        config.Url = "https://test.local";
//        var response = new HttpResponseMessage
//        {
//            Content = new StringContent("\"test\""),
//            StatusCode = HttpStatusCode.Accepted
//        };
//        var getTestRequest = new GetTestRequest(id);
//        var expectedUrl = $"{config.Url}/{getTestRequest.GetUrl}";
//        var httpMessageHandler = MessageHandler.SetupMessageHandlerMock(response, expectedUrl);
//        var client = new HttpClient(httpMessageHandler.Object);
//        var clientFactory = new Mock<IHttpClientFactory>();
//        clientFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);
//        var actual = new InternalApiClient<TestInternalApiConfiguration>(clientFactory.Object, config, azureClientCredentialHelper.Object);

//        //Act
//        await actual.Get<string>(getTestRequest);

//        //Assert
//        httpMessageHandler.Protected()
//            .Verify<Task<HttpResponseMessage>>(
//                "SendAsync", Times.Once(),
//                ItExpr.Is<HttpRequestMessage>(c =>
//                    c.Method.Equals(HttpMethod.Get)
//                    && c.RequestUri.AbsoluteUri.Equals(expectedUrl)
//                    && c.Headers.Authorization.Scheme.Equals("Bearer")
//                    && c.Headers.FirstOrDefault(h => h.Key.Equals("X-Version")).Value.FirstOrDefault() == "1.0"
//                    && c.Headers.Authorization.Parameter.Equals(authToken)),
//                ItExpr.IsAny<CancellationToken>()
//            );
//    }

//    [Test, AutoData]
//    public async Task Then_The_Response_Is_Correctly_Deserialized(string authToken, int id, string responseValue, TestInternalApiConfiguration config)
//    {
//        //Arrange
//        var azureClientCredentialHelper = new Mock<IAzureClientCredentialHelper>();
//        azureClientCredentialHelper.Setup(x => x.GetAccessTokenAsync(config.Identifier)).ReturnsAsync(authToken);
//        config.Url = "https://test.local";
//        var testObject = JsonSerializer.Serialize(new TestResponse { MyResponse = responseValue });
//        var response = new HttpResponseMessage
//        {
//            Content = new StringContent(testObject, Encoding.UTF8, "application/json"),
//            StatusCode = HttpStatusCode.OK
//        };
//        var getTestRequest = new GetTestRequest(id);
//        var expectedUrl = $"{config.Url}/{getTestRequest.GetUrl}";
//        var httpMessageHandler = MessageHandler.SetupMessageHandlerMock(response, expectedUrl);
//        var client = new HttpClient(httpMessageHandler.Object);
//        var clientFactory = new Mock<IHttpClientFactory>();
//        clientFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);
//        var actual = new InternalApiClient<TestInternalApiConfiguration>(clientFactory.Object, config, azureClientCredentialHelper.Object);

//        //Act
//        var result = await actual.Get<TestResponse>(getTestRequest);

//        //Assert
//        result.Should().NotBeNull();
//        result.MyResponse.Should().Be(responseValue);
//    }
//    [Test, AutoData]
//    public async Task Then_If_Returns_Not_Found_Result_Returns_Default(int id,
//           TestInternalApiConfiguration config)
//    {
//        //Arrange
//        config.Url = "https://test.local";
//        var configuration = config;
//        var response = new HttpResponseMessage
//        {
//            Content = new StringContent(""),
//            StatusCode = HttpStatusCode.NotFound
//        };
//        var getTestRequest = new GetTestRequestNoVersion(id);
//        var expectedUrl = $"{config.Url}/{getTestRequest.GetUrl}";
//        var httpMessageHandler = MessageHandler.SetupMessageHandlerMock(response, expectedUrl);
//        var client = new HttpClient(httpMessageHandler.Object);
//        var clientFactory = new Mock<IHttpClientFactory>();
//        clientFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);

//        var actual = new InternalApiClient<TestInternalApiConfiguration>(clientFactory.Object, configuration, Mock.Of<IAzureClientCredentialHelper>());

//        //Act
//        var actualResult = await actual.Get<string>(getTestRequest);

//        //Assert
//        Assert.That(actualResult, Is.Null);
//    }

//       private class GetTestRequestNoVersion : IApiRequest
//        {
//            private readonly int _id;

//            public GetTestRequestNoVersion (int id)
//            {
//                _id = id;
//            }
//            public string GetUrl => $"test-url/get{_id}";
//        }
//    private class GetTestRequest : IApiRequest
//    {
//        private readonly int _id;

//        public GetTestRequest(int id)
//        {
//            _id = id;
//        }
//        public string GetUrl => $"test-url/get{_id}";
//    }

//    private class TestResponse
//    {
//        public string MyResponse { get; set; }
//    }
//}