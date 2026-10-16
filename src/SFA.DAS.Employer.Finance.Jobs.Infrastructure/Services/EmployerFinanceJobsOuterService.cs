using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests.FundingProjection;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses.FundingProjection;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;

public class EmployerFinanceJobsOuterService(IEmployerFinanceJobsOuterApiClient apiClient,
    ILogger<EmployerFinanceJobsOuterService> logger) : IEmployerFinanceJobsOuterService
{
    public async Task<ImportCommittedLearnersResponse> ImportCommittedLearnersAsync(long accountId, CancellationToken cancellationToken)
    {
        logger.LogInformation("Importing committed learners.");

        try
        {
            return await apiClient.Post<ImportCommittedLearnersResponse>(
                new ImportCommittedLearnersRequest(new ImportCommittedLearnersRequestData(accountId)), cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unable to get committed learners for ImportCommittedLearnersRequest from EmployerFinanceJobs API.");
            throw;
        }
    }

    public async Task PostJobAsync(Guid jobId, PostJobRequestData payload, CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating job {JobId} with state {JobState}.", jobId, payload.JobState);
        try
        {
            await apiClient.Post(new PostJobRequest(jobId, payload), cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unable to update job {JobId} from EmployerFinanceJobs API.", jobId);
            throw;
        }
    }
}
