using Microsoft.Extensions.Logging;
using SFA.DAS.Api.Common.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces.Services;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Abstractions;

public abstract class BaseApiClient : IApiClient
{
    protected readonly HttpClient HttpClient;
    protected readonly IAzureClientCredentialHelper CredentialHelper;
    protected readonly ILogger Logger;
    protected readonly string BaseUrl;
    protected readonly string Identifier;
    private readonly JsonSerializerOptions _jsonOptions;

    protected BaseApiClient(HttpClient httpClient,
                            IAzureClientCredentialHelper credentialHelper,
                            ILogger logger,
                            IApiConfiguration configuration)
    {
        HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        CredentialHelper = credentialHelper ?? throw new ArgumentNullException(nameof(credentialHelper));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));
            
        BaseUrl = configuration.Url ?? throw new ArgumentNullException(nameof(configuration.Url));
        Identifier = configuration.Identifier ?? throw new ArgumentNullException(nameof(configuration.Identifier));

        HttpClient.BaseAddress = new Uri(BaseUrl);
        HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    protected async Task EnsureAuthenticatedAsync()
    {
        if (HttpClient.DefaultRequestHeaders.Authorization == null && !string.IsNullOrEmpty(Identifier))
        {
            var token = await CredentialHelper.GetAccessTokenAsync(Identifier);
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }       
    }

    public virtual async Task<TResponse> Get<TResponse>(IGetApiRequest request)
    {
        await EnsureAuthenticatedAsync();

        var response = await HttpClient.GetAsync(request.GetUrl);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<TResponse>(content, _jsonOptions);
    }

    public virtual async Task<IEnumerable<TResponse>> GetAll<TResponse>(IGetAllApiRequest request)
    {
        await EnsureAuthenticatedAsync();

        var response = await HttpClient.GetAsync(request.GetUrl);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<IEnumerable<TResponse>>(content, _jsonOptions);
    }

    public virtual async Task<HttpStatusCode> GetResponseCode(IGetApiRequest request)
    {
        await EnsureAuthenticatedAsync();

        var response = await HttpClient.GetAsync(request.GetUrl);
        return response.StatusCode;
    }

    public virtual async Task<ApiResponse<TResponse>> GetWithResponseCode<TResponse>(IGetApiRequest request)
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
