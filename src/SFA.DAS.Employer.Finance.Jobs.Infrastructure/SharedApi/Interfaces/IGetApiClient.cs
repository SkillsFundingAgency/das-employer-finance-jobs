using System.Net;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces
{
    public interface IGetApiClient<T>
    {
        Task<TResponse> Get<TResponse>(IApiRequest request);
        Task<HttpStatusCode> GetResponseCode(IApiRequest request);
        Task<ApiResponse<TResponse>> GetWithResponseCode<TResponse>(IApiRequest request);
        Task<TResponse> Post<TResponse>(IApiRequest request);
        Task<ApiResponse<TResponse>> PostWithResponseCode<TResponse>(IApiRequest request);
    }
}