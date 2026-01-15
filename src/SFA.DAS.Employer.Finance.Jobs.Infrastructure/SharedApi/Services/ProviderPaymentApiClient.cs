using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;
using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Services
{
    [ExcludeFromCodeCoverage]
    public class ProviderPaymentApiClient : IProviderPaymentApiClient<ProviderPaymentApiConfiguration>
    {
        private readonly IInternalApiClient<ProviderPaymentApiConfiguration> _apiClient;

        public ProviderPaymentApiClient(IInternalApiClient<ProviderPaymentApiConfiguration> apiClient)
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

        public Task<TResponse> Post<TResponse>(IApiRequest request)
        {
            return _apiClient.Post<TResponse>(request);
        }

        public Task<ApiResponse<TResponse>> PostWithResponseCode<TResponse>(IApiRequest request)
        {
            return _apiClient.PostWithResponseCode<TResponse>(request);
        }
    }
}