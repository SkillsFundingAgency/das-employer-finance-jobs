using Microsoft.Extensions.Options;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi;

public class OuterApiClient<TConfig>(HttpClient httpClient, IOptions<TConfig> config) : IOuterApiClient where TConfig : class, IOuterApiConfiguration
{
    private readonly IOuterApiConfiguration _config = config.Value;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public Task<TResponse> Get<TResponse>(IApiRequest request, CancellationToken cancellationToken = default)
        => SendAsync<TResponse>(HttpMethod.Get, request, cancellationToken);

    public Task<ApiResponse<TResponse>> GetWithResponseCodeAsync<TResponse>(IApiRequest request, CancellationToken cancellationToken = default)
        => SendWithResponseCodeAsync<TResponse>(HttpMethod.Get, request, cancellationToken);

    public Task<TResponse> Put<TResponse>(IApiRequest request, CancellationToken cancellationToken = default)
        => SendAsync<TResponse>(HttpMethod.Put, request, cancellationToken);

    public Task<ApiResponse<TResponse>> PutWithResponseCode<TResponse>(IApiRequest request, CancellationToken cancellationToken = default)
        => SendWithResponseCodeAsync<TResponse>(HttpMethod.Put, request, cancellationToken);

    public Task<TResponse> Post<TResponse>(IApiRequest request, CancellationToken cancellationToken = default)
        => SendAsync<TResponse>(HttpMethod.Post, request, cancellationToken);

    public async Task Post(IApiRequest request, CancellationToken cancellationToken = default)
        => await SendWithResponseCodeAsync<object>(HttpMethod.Post, request, cancellationToken);

    public Task<ApiResponse<TResponse>> PostWithResponseCodeAsync<TResponse>(IApiRequest request, CancellationToken cancellationToken = default)
        => SendWithResponseCodeAsync<TResponse>(HttpMethod.Post, request, cancellationToken);

    private async Task<TResponse> SendAsync<TResponse>(HttpMethod method, IApiRequest request, CancellationToken cancellationToken)
    {
        using var requestMessage = BuildRequestMessage(method, request);
        var response = await httpClient.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return default;

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions, cancellationToken).ConfigureAwait(false);
    }

    private async Task<ApiResponse<TResponse>> SendWithResponseCodeAsync<TResponse>(HttpMethod method, IApiRequest request, CancellationToken cancellationToken)
    {
        using var requestMessage = BuildRequestMessage(method, request);
        var response = await httpClient.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            var data = await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions, cancellationToken).ConfigureAwait(false);
            return new ApiResponse<TResponse>(data, response.StatusCode, string.Empty);
        }

        var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return new ApiResponse<TResponse>(default, response.StatusCode, errorContent);
    }

    private HttpRequestMessage BuildRequestMessage(HttpMethod method, IApiRequest request)
    {
        var message = new HttpRequestMessage(method, request.GetUrl);

        if (request.Data is not null)
            message.Content = new StringContent(
                JsonSerializer.Serialize(request.Data),
                System.Text.Encoding.UTF8,
                "application/json");

        message.Headers.Add("Ocp-Apim-Subscription-Key", _config.Key);
        message.Headers.Add("X-Version", "1");

        return message;
    }
}