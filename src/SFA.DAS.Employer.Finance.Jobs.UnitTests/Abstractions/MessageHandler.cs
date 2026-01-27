using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Abstractions;
public static class MessageHandler
{
    public static Mock<HttpMessageHandler> SetupMessageHandlerMock(HttpResponseMessage response, string baseUrl, string httpMethod = "get")
    {
        var method = HttpMethod.Get;
        if (httpMethod.Equals("get", StringComparison.CurrentCultureIgnoreCase))
        {
            method = HttpMethod.Get;
        }

        var httpMessageHandler = new Mock<HttpMessageHandler>();
        httpMessageHandler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync",ItExpr.Is<HttpRequestMessage>(c => c.Method.Equals(method) && c.RequestUri.AbsoluteUri.Equals(baseUrl)),
                ItExpr.IsAny<CancellationToken>()).ReturnsAsync((HttpRequestMessage request, CancellationToken token) => response);
        return httpMessageHandler;
    }
}
