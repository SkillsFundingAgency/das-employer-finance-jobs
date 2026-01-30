using SFA.DAS.Api.Common.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;
using System.Net.Http.Headers;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi
{
    public class InternalApiClient<T> : ApiClient<T>, IInternalApiClient<T> where T : IInternalApiConfiguration
    {
        private readonly IAzureClientCredentialHelper _azureClientCredentialHelper;

        public InternalApiClient(
            IHttpClientFactory httpClientFactory,
            T apiConfiguration,
            IAzureClientCredentialHelper azureClientCredentialHelper) : base(httpClientFactory, apiConfiguration)
        {
            _azureClientCredentialHelper = azureClientCredentialHelper;
        }

        protected override async Task AddAuthenticationHeader(HttpRequestMessage httpRequestMessage)
        {
            if (!string.IsNullOrEmpty(Configuration.Identifier))
            {
                var accessToken = await _azureClientCredentialHelper.GetAccessTokenAsync(Configuration.Identifier);
                httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }
        }
    }
}
