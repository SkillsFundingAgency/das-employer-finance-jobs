using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;

public class HmrcTokenProvider(
    IHttpClientFactory httpClientFactory,
    HmrcConfiguration configuration,
    IHmrcClock hmrcClock,
    ILogger<HmrcTokenProvider> logger) : IHmrcTokenProvider
{
    private readonly SemaphoreSlim _tokenLock = new(1, 1);
    private string? _accessToken;
    private DateTimeOffset _expiresAt = DateTimeOffset.MinValue;

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        if (TokenIsValid())
        {
            return _accessToken!;
        }

        await _tokenLock.WaitAsync(cancellationToken);

        try
        {
            if (TokenIsValid())
            {
                return _accessToken!;
            }

            var client = httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(configuration.BaseUrl);

            var tokenRequest = $"oauth/token?client_secret={Uri.EscapeDataString(configuration.ClientSecret)}&client_id={Uri.EscapeDataString(configuration.ClientId)}&grant_type=client_credentials&scopes={Uri.EscapeDataString(configuration.Scope)}";
            using var response = await client.PostAsync(tokenRequest, new StringContent(string.Empty), cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException($"Unable to retrieve an HMRC access token. Status code: {response.StatusCode}. Response: {responseBody}");
            }

            var tokenResponse = await response.Content.ReadFromJsonAsync<HmrcAccessTokenResponse>(cancellationToken: cancellationToken);

            if (string.IsNullOrWhiteSpace(tokenResponse?.AccessToken))
            {
                throw new InvalidOperationException("Unable to retrieve an HMRC access token because the response did not include an access token.");
            }

            _accessToken = tokenResponse.AccessToken;
            _expiresAt = hmrcClock.UtcNow.AddSeconds(Math.Max(tokenResponse.ExpiresIn - 60, 60));

            logger.LogInformation("Retrieved a fresh HMRC access token. Token expires at {ExpiresAt}.", _expiresAt);

            return _accessToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private bool TokenIsValid()
    {
        return !string.IsNullOrWhiteSpace(_accessToken) && hmrcClock.UtcNow < _expiresAt;
    }

    private sealed class HmrcAccessTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; } = 3600;
    }
}
