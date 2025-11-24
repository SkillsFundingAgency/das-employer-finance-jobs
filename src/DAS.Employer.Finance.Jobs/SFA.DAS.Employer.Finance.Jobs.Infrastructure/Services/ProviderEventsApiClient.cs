using Microsoft.Extensions.Logging;
using SFA.DAS.Api.Common.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Abstractions;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces.Services;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using System.Net;
using System.Text.Json;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;

public class ProviderEventsApiClient : BaseApiClient, IProviderEventsApiClient
{
    private readonly JsonSerializerOptions _jsonOptions;

    public ProviderEventsApiClient(IHttpClientFactory httpClientFactory, IAzureClientCredentialHelper credentialHelper, ProviderEventsApiConfiguration configuration, ILogger<ProviderEventsApiClient> logger)
                                  : base( httpClientFactory.CreateClient(), credentialHelper, logger, configuration.Url, configuration.Identifier)  
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<TResponse> Get<TResponse>(IGetApiRequest request)
    {
        await EnsureAuthenticatedAsync();

        var response = await HttpClient.GetAsync(request.GetUrl);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<TResponse>(content, _jsonOptions);
    }

    public async Task<IEnumerable<TResponse>> GetAll<TResponse>(IGetAllApiRequest request)
    {
        await EnsureAuthenticatedAsync();

        var response = await HttpClient.GetAsync(request.GetUrl);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<IEnumerable<TResponse>>(content, _jsonOptions);
    }

    public async Task<HttpStatusCode> GetResponseCode(IGetApiRequest request)
    {
        await EnsureAuthenticatedAsync();

        var response = await HttpClient.GetAsync(request.GetUrl);
        return response.StatusCode;
    }

    public async Task<ApiResponse<TResponse>> GetWithResponseCode<TResponse>(IGetApiRequest request)
    {
        await EnsureAuthenticatedAsync();

        var response = await HttpClient.GetAsync(request.GetUrl);
        var content = await response.Content.ReadAsStringAsync();

        return new ApiResponse<TResponse>
        {
            StatusCode = response.StatusCode,
            Body = response.IsSuccessStatusCode
                ? JsonSerializer.Deserialize<TResponse>(content, _jsonOptions)
                : default(TResponse)
        };
    }
}
