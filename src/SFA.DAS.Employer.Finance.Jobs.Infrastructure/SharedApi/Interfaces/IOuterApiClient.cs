using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;

public interface IOuterApiClient
{
    Task<TResponse> Get<TResponse>(IApiRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<TResponse>> GetWithResponseCodeAsync<TResponse>(IApiRequest request, CancellationToken cancellationToken = default);
    Task<TResponse> Put<TResponse>(IApiRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<TResponse>> PostWithResponseCodeAsync<TResponse>(IApiRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<TResponse>> PutWithResponseCode<TResponse>(IApiRequest request, CancellationToken cancellationToken = default);
    Task Post(IApiRequest request, CancellationToken cancellationToken = default);
    Task<TResponse> Post<TResponse>(IApiRequest request, CancellationToken cancellationToken = default);
}