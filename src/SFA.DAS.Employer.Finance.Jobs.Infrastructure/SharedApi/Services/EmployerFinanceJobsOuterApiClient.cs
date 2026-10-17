using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;
using System.Diagnostics.CodeAnalysis;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Services;

[ExcludeFromCodeCoverage]
public class EmployerFinanceJobsOuterApiClient(OuterApiClient<EmployerFinanceJobsOuterApiConfiguration> apiClient) : IEmployerFinanceJobsOuterApiClient
{
    public async Task<TResponse> Get<TResponse>(IApiRequest request, CancellationToken cancellationToken = default)
    {
        return await apiClient.Get<TResponse>(request, cancellationToken);
    }

    public async Task<ApiResponse<TResponse>> GetWithResponseCodeAsync<TResponse>(IApiRequest request, CancellationToken cancellationToken = default)
    {
        return await apiClient.GetWithResponseCodeAsync<TResponse>(request, cancellationToken);
    }

    public async Task<TResponse> Put<TResponse>(IApiRequest request, CancellationToken cancellationToken = default)
    {
        return await apiClient.Put<TResponse>(request, cancellationToken);
    }

    public async Task<ApiResponse<TResponse>> PostWithResponseCodeAsync<TResponse>(IApiRequest request, CancellationToken cancellationToken = default)
    {
        return await apiClient.PostWithResponseCodeAsync<TResponse>(request, cancellationToken);
    }

    public async Task<ApiResponse<TResponse>> PutWithResponseCode<TResponse>(IApiRequest request, CancellationToken cancellationToken = default)
    {
        return await apiClient.PutWithResponseCode<TResponse>(request, cancellationToken);
    }

    public async Task Post(IApiRequest request, CancellationToken cancellationToken = default)
    {
        await apiClient.Post(request, cancellationToken);
    }

    public async Task<TResponse> Post<TResponse>(IApiRequest request, CancellationToken cancellationToken = default)
    {
        return await apiClient.Post<TResponse>(request, cancellationToken);
    }
}