using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Extensions;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using System.Net;
using System.Text.Json;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;

public class EmployerFinanceOuterApiClient(
    IHttpClientFactory httpClientFactory,
    EmployerFinanceOuterApiConfiguration configuration,
    ILogger<EmployerFinanceOuterApiClient> logger) : IEmployerFinanceOuterApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public Task<ProviderDetails?> GetProvider(long ukprn)
    {
        return Get<ProviderDetails>(new GetOuterApiProviderRequest(ukprn));
    }

    public Task<StandardsResponse?> GetStandards()
    {
        return Get<StandardsResponse>(new GetOuterApiStandardsRequest());
    }

    public Task<FrameworksResponse?> GetFrameworks()
    {
        return Get<FrameworksResponse>(new GetOuterApiFrameworksRequest());
    }

    private async Task<T?> Get<T>(IApiRequest request)
    {
        try
        {
            var client = httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(configuration.BaseUrl);

            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, request.GetUrl);
            httpRequestMessage.Headers.Add("X-Version", request.Version);

            if (!string.IsNullOrWhiteSpace(configuration.Key))
            {
                httpRequestMessage.Headers.Add("Ocp-Apim-Subscription-Key", configuration.Key);
            }

            var response = await client.SendAsync(httpRequestMessage).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return default;
            }

            await response.EnsureSuccessStatusCodeIncludeContentInException().ConfigureAwait(false);

            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return string.IsNullOrWhiteSpace(json)
                ? default
                : JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unable to get Employer Finance Outer API data from {Url}.", request.GetUrl);
            return default;
        }
    }
}
