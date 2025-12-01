using System.Net;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces.Services;
public interface IApiClient
{
    Task<TResponse> Get<TResponse>(IGetApiRequest request);
    Task<IEnumerable<TResponse>> GetAll<TResponse>(IGetAllApiRequest request);
    Task<HttpStatusCode> GetResponseCode(IGetApiRequest request);
    Task<ApiResponse<TResponse>> GetWithResponseCode<TResponse>(IGetApiRequest request);
}