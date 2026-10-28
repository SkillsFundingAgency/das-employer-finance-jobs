using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests.FundingProjection;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses.FundingProjection;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;

public class EmployerFinanceJobsOuterService(IEmployerFinanceJobsOuterApiClient apiClient,
    ILogger<EmployerFinanceJobsOuterService> logger) : IEmployerFinanceJobsOuterService
{
    public async Task<ImportCommittedLearnersResponse> ImportCommittedLearnersAsync(long accountId, DateTime lastSuccessfulImportDate, CancellationToken cancellationToken)
    {
        logger.LogInformation("Importing committed learners.");

        try
        {
            var response = await apiClient.PostWithResponseCodeAsync<ImportCommittedLearnersResponse>(
                new ImportCommittedLearnersRequest(new ImportCommittedLearnersRequestData(accountId, lastSuccessfulImportDate)), cancellationToken);

            EnsureSuccess(response, "import committed learners");

            return response.Body
                ?? throw new InvalidOperationException("EmployerFinanceJobs API returned an empty response when importing committed learners.");
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
            var response = await apiClient.PostWithResponseCodeAsync<object>(new PostJobRequest(jobId, payload), cancellationToken);

            EnsureSuccess(response, $"update job {jobId}");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unable to update job {JobId} from EmployerFinanceJobs API.", jobId);
            throw;
        }
    }

    private static void EnsureSuccess<TResponse>(ApiResponse<TResponse> response, string operation)
    {
        if ((int)response.StatusCode is < 200 or > 299)
        {
            throw new HttpRequestException(
                $"EmployerFinanceJobs API failed to {operation}: {(int)response.StatusCode} ({response.StatusCode}). {response.ErrorContent}",
                null,
                response.StatusCode);
        }
    }
}
