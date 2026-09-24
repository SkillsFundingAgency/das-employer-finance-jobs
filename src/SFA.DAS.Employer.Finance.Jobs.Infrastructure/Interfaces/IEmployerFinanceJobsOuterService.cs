using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses.FundingProjection;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

public interface IEmployerFinanceJobsOuterService
{
    Task<ImportCommittedLearnersResponse> ImportCommittedLearnersAsync(CancellationToken cancellationToken);
    Task<UpdateCommittedLearnersCostResponse> UpdateCommittedLearnersCostAsync(CancellationToken cancellationToken);
    Task<RecalculateFundingProjectionResponse> RecalculateFundingProjectionAsync(CancellationToken cancellationToken);
}