using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests.FundingProjection;

internal class UpdateCommittedLearnersCostRequest : IApiRequest
{
    public string GetUrl => "learners/update";
    public object? Data { get; } = null;
}