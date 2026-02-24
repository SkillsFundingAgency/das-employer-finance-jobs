using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces
{
    public interface IApiClient<T> : IGetApiClient<T>
    {
        Task<ApiResponse<TResponse>> PostWithResponseCode<TResponse>(IPostApiRequest request, bool includeResponse = true);
        Task<ApiResponse<TResponse>> PostWithResponseCode<TData, TResponse>(IPostApiRequest<TData> request, bool includeResponse = true);
    }
}