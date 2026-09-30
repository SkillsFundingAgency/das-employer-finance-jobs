using System.Net;
using System.Diagnostics.CodeAnalysis;


namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Extensions;
[ExcludeFromCodeCoverage]
public class HttpRequestContentException : HttpRequestException
{
    public string ErrorContent { get; set; }
    public HttpStatusCode StatusCode { get; set; }

    public HttpRequestContentException(string message, HttpStatusCode statusCode) : this(message, statusCode, "")
    {
    }

    public HttpRequestContentException(string message, HttpStatusCode statusCode, string errorContent)
        : base(BuildMessage(message, errorContent))
    {
        StatusCode = statusCode;
        ErrorContent = errorContent;
    }

    private static string BuildMessage(string message, string errorContent)
    {
        if (string.IsNullOrWhiteSpace(errorContent))
        {
            return message;
        }

        return $"{message}. Content: {errorContent}";
    }
}