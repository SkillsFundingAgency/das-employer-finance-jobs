using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests.FundingProjection;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses.FundingProjection;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;

public class EmployerFinanceJobsOuterService(IEmployerFinanceJobsOuterApiClient apiClient,
    ILogger<EmployerFinanceJobsOuterService> logger) : IEmployerFinanceJobsOuterService
{
    public async Task<ImportCommittedLearnersResponse> ImportCommittedLearnersAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Importing committed learners.");

        try
        {
            return await apiClient.Post<ImportCommittedLearnersResponse>(new ImportCommittedLearnersRequest(), cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unable to get committed learners for ImportCommittedLearnersRequest from EmployerFinanceJobs API.");
            throw;
        }
    }

    public async Task<UpdateCommittedLearnersCostResponse> UpdateCommittedLearnersCostAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating committed learners cost.");
        try
        {
            return await apiClient.Post<UpdateCommittedLearnersCostResponse>(new UpdateCommittedLearnersCostRequest(), cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unable to update committed learners cost from EmployerFinanceJobs API.");
            throw;
        }
    }

    public async Task<RecalculateFundingProjectionResponse> RecalculateFundingProjectionAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Recalculating funding projection.");
        try
        {
            return await apiClient.Post<RecalculateFundingProjectionResponse>(new RecalculateFundingProjectionRequest(), cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unable to recalculate funding projection from EmployerFinanceJobs API.");
            throw;
        }
    }
}