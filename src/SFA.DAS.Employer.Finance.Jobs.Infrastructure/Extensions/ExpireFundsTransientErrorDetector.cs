using System.Net;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Extensions;

public static class ExpireFundsTransientErrorDetector
{
    public static bool IsTransient(Exception exception) =>
        exception switch
        {
            HttpRequestContentException contentException => IsTransient(contentException.StatusCode),
            HttpRequestException requestException when requestException.StatusCode.HasValue =>
                IsTransient(requestException.StatusCode.Value),
            HttpRequestException => true,
            TimeoutException => true,
            TaskCanceledException => true,
            _ => false
        };

    private static bool IsTransient(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.RequestTimeout
            or HttpStatusCode.TooManyRequests
            or HttpStatusCode.InternalServerError
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;
}
