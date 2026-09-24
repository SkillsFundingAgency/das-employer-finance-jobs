using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests.FundingProjection;

public sealed record ImportCommittedLearnersRequest : IApiRequest
{
    public string GetUrl => "learners/import";
    public object? Data { get; } = null;
}