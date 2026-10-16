using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses.FundingProjection;

using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests.FundingProjection;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

public interface IEmployerFinanceJobsOuterService
{
    Task<ImportCommittedLearnersResponse> ImportCommittedLearnersAsync(long accountId, CancellationToken cancellationToken);
    Task PostJobAsync(Guid jobId, PostJobRequestData payload, CancellationToken cancellationToken);
}
