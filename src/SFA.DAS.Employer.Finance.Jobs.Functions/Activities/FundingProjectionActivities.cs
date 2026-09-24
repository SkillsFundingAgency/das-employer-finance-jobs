using Microsoft.Azure.Functions.Worker;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses.FundingProjection;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.Activities;

public class FundingProjectionActivities(IEmployerFinanceJobsOuterService employerFinanceJobsOuterService)
{
    [Function(nameof(ImportCommittedLearnersActivity))]
    public Task<ImportCommittedLearnersResponse> ImportCommittedLearnersActivity(
        [ActivityTrigger] object? input,
        CancellationToken cancellationToken) =>
        employerFinanceJobsOuterService.ImportCommittedLearnersAsync(cancellationToken);

    [Function(nameof(UpdateCommittedLearnersCostActivity))]
    public Task<UpdateCommittedLearnersCostResponse> UpdateCommittedLearnersCostActivity(
        [ActivityTrigger] object? input,
        CancellationToken cancellationToken) =>
        employerFinanceJobsOuterService.UpdateCommittedLearnersCostAsync(cancellationToken);

    [Function(nameof(RecalculateFundingProjectionActivity))]
    public Task<RecalculateFundingProjectionResponse> RecalculateFundingProjectionActivity(
        [ActivityTrigger] object? input,
        CancellationToken cancellationToken) =>
        employerFinanceJobsOuterService.RecalculateFundingProjectionAsync(cancellationToken);
}
