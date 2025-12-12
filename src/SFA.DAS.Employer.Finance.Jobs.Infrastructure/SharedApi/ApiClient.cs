using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi
{
    [ExcludeFromCodeCoverage]
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

