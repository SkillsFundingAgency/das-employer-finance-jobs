using Microsoft.Azure.Functions.Worker;
using SFA.DAS.Employer.Finance.Jobs.Functions.FundingProjection.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses.FundingProjection;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.FundingProjection.Activities;

public class FundingProjectionActivities(IEmployerFinanceJobsOuterService employerFinanceJobsOuterService)
{
    [Function(nameof(ImportCommittedLearnersActivity))]
    public Task<ImportCommittedLearnersResponse> ImportCommittedLearnersActivity(
        [ActivityTrigger] ImportLearnersCostEvent input,
        CancellationToken cancellationToken) =>
        employerFinanceJobsOuterService.ImportCommittedLearnersAsync(input.AccountId, cancellationToken);

    [Function(nameof(PostJobActivity))]
    public Task PostJobActivity(
        [ActivityTrigger] PostJobActivityInput input,
        CancellationToken cancellationToken) =>
        employerFinanceJobsOuterService.PostJobAsync(input.JobId, input.Payload, cancellationToken);
}
