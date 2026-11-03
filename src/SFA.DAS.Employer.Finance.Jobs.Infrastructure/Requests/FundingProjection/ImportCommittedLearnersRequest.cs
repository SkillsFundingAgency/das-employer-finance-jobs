using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests.FundingProjection;

public sealed record ImportCommittedLearnersRequest(ImportCommittedLearnersRequestData Payload) : IApiRequest
{
    public string GetUrl => "api/apprenticeship/import-learners";
    public object? Data { get; } = Payload;
}

public sealed record ImportCommittedLearnersRequestData(long AccountId);