using Microsoft.Extensions.Logging;
using SFA.DAS.Api.Common.Interfaces;
using System.Net.Http.Headers;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Abstractions;

public abstract class BaseApiClient
{
    protected readonly HttpClient HttpClient;
    protected readonly IAzureClientCredentialHelper CredentialHelper;
    protected readonly ILogger Logger;
    protected readonly string BaseUrl;
    protected readonly string Identifier;

    protected BaseApiClient(
        HttpClient httpClient,
        IAzureClientCredentialHelper credentialHelper,
        ILogger logger,
        string baseUrl,
        string identifier)
    {
        HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        CredentialHelper = credentialHelper ?? throw new ArgumentNullException(nameof(credentialHelper));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        BaseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
        Identifier = identifier ?? throw new ArgumentNullException(nameof(identifier));

        HttpClient.BaseAddress = new System.Uri(BaseUrl);
        HttpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }

    protected async Task EnsureAuthenticatedAsync()
    {
        if (HttpClient.DefaultRequestHeaders.Authorization == null && !string.IsNullOrEmpty(Identifier))
        {
            var token = await CredentialHelper.GetAccessTokenAsync(Identifier);
            HttpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }       
    }
}
