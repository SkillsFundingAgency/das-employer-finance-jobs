using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;

public class CommitmentsApiClient(
    IInternalApiClient<CommitmentsApiConfiguration> apiClient,
    ILogger<CommitmentsApiClient> logger) : ICommitmentsApiClient
{
    public async Task<ApprenticeshipDetails?> GetApprenticeship(long apprenticeshipId)
    {
        try
        {
            return await apiClient.Get<ApprenticeshipDetails>(new GetApprenticeshipRequest(apprenticeshipId));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unable to get apprenticeship details for ApprenticeshipId {ApprenticeshipId} from Commitments API.", apprenticeshipId);
            return null;
        }
    }
}
