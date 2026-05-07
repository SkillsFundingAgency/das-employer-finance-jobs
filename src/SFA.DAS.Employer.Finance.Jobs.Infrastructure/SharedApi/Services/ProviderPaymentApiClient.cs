using System.Diagnostics.CodeAnalysis;
using System.Net;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;


namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Services;

[ExcludeFromCodeCoverage]
public class ProviderPaymentApiClient : IProviderPaymentApiClient<ProviderEventsApiConfiguration>
{
    private readonly IInternalApiClient<ProviderEventsApiConfiguration> _apiClient;

    public ProviderPaymentApiClient(IInternalApiClient<ProviderEventsApiConfiguration> apiClient)
    {
        _apiClient = apiClient;
    }
    public Task<TResponse> Get<TResponse>(IApiRequest request)
    {
        return _apiClient.Get<TResponse>(request);
    }

    public Task<IEnumerable<TResponse>> GetAll<TResponse>(IGetAllApiRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<HttpStatusCode> GetResponseCode(IApiRequest request)
    {
        return _apiClient.GetResponseCode(request);
    }

    public Task<ApiResponse<TResponse>> GetWithResponseCode<TResponse>(IApiRequest request)
    {
        return _apiClient.GetWithResponseCode<TResponse>(request);
    }

    public Task Post<TBody>(string url, TBody body)
    {
        return _apiClient.Post(url, body);
    }

    public Task<TResponse> Post<TResponse>(IApiRequest request)
    {
        return _apiClient.Post<TResponse>(request);
    }

    public Task<ApiResponse<TResponse>> PostWithResponseCode<TResponse>(IApiRequest request)
    {
        return _apiClient.PostWithResponseCode<TResponse>(request);
    }
}
