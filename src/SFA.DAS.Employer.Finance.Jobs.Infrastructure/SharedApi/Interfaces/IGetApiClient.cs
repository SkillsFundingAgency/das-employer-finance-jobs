using System.Net;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces
{
    public interface IGetApiClient<T>
    {
        Task<TResponse> Get<TResponse>(IGetApiRequest request);
        Task<HttpStatusCode> GetResponseCode(IGetApiRequest request);
        Task<ApiResponse<TResponse>> GetWithResponseCode<TResponse>(IGetApiRequest request);
    }
}