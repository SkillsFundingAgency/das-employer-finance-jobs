using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;
using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Services
{
    [ExcludeFromCodeCoverage]
    public class FinanceApiClient : DelegatingInternalApiClient<FinanceApiConfiguration>, IFinanceApiClient<FinanceApiConfiguration>
    {
        public FinanceApiClient(IInternalApiClient<FinanceApiConfiguration> apiClient)
            : base(apiClient)
        {
        }

        public Task<TResponse> Get<TResponse>(IApiRequest request)
        {
            return base.Get<TResponse>(request);
        }

        public Task<IEnumerable<TResponse>> GetAll<TResponse>(IGetAllApiRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<HttpStatusCode> GetResponseCode(IApiRequest request)
        {
            return base.GetResponseCode(request);
        }

        public Task<ApiResponse<TResponse>> GetWithResponseCode<TResponse>(IApiRequest request)
        {
            return base.GetWithResponseCode<TResponse>(request);
        }

        public Task Post<TBody>(string url, TBody body)
        {
            return base.Post(url, body);
        }

        public Task<TResponse> Post<TResponse>(IApiRequest request)
        {
            return base.Post<TResponse>(request);
        }

        public Task<ApiResponse<TResponse>> PostWithResponseCode<TResponse>(IApiRequest request)
        {
            return base.PostWithResponseCode<TResponse>(request);
        }
    }
}
