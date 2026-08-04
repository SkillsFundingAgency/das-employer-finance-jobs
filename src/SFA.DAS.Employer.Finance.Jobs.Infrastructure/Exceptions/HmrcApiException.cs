using System.Net;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Exceptions;

public class HmrcApiException(HttpStatusCode statusCode, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}
