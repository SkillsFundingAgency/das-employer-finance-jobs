using System.Net;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;
public class ApiResponse<T>
{
    public T Body { get; set; }
    public HttpStatusCode StatusCode { get; set; }
}