using System.Text.Json;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi
{
    public abstract class ApiClient<T> : GetApiClient<T>, IApiClient<T> where T : IApiConfiguration
    {
        public static readonly JsonSerializerOptions JsonSerializationOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };
        public ApiClient(
            IHttpClientFactory httpClientFactory,
            T apiConfiguration) : base(httpClientFactory, apiConfiguration)
        {
        }         
    }
}

