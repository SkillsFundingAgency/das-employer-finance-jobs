using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;
using System.Net;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces.Services;

public interface IProviderEventsApiClient
{    Task<TResponse> Get<TResponse>(IGetApiRequest request);
    Task<IEnumerable<TResponse>> GetAll<TResponse>(IGetAllApiRequest request);
    Task<HttpStatusCode> GetResponseCode(IGetApiRequest request);
    Task<ApiResponse<TResponse>> GetWithResponseCode<TResponse>(IGetApiRequest request);
}

