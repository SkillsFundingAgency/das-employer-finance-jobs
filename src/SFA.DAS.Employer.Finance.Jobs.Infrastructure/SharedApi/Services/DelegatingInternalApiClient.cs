using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;
using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Services;

[ExcludeFromCodeCoverage]
public abstract class DelegatingInternalApiClient<TConfiguration>
{
    private readonly IInternalApiClient<TConfiguration> _apiClient;

    protected DelegatingInternalApiClient(IInternalApiClient<TConfiguration> apiClient)
    {
        _apiClient = apiClient;
    }

    protected Task<TResponse> Get<TResponse>(IApiRequest request)
    {
        return _apiClient.Get<TResponse>(request);
    }

    protected Task<HttpStatusCode> GetResponseCode(IApiRequest request)
    {
        return _apiClient.GetResponseCode(request);
    }

    protected Task<ApiResponse<TResponse>> GetWithResponseCode<TResponse>(IApiRequest request)
    {
        return _apiClient.GetWithResponseCode<TResponse>(request);
    }

    protected Task Post<TBody>(string url, TBody body)
    {
        return _apiClient.Post(url, body);
    }

    protected Task<TResponse> Post<TResponse>(IApiRequest request)
    {
        return _apiClient.Post<TResponse>(request);
    }

    protected Task<ApiResponse<TResponse>> PostWithResponseCode<TResponse>(IApiRequest request)
    {
        return _apiClient.PostWithResponseCode<TResponse>(request);
    }
}
