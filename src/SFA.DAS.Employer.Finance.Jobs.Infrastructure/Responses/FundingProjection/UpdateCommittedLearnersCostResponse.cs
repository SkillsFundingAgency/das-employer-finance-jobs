namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses.FundingProjection;

public sealed record UpdateCommittedLearnersCostResponse
{
    public int TotalRecords { get; init; }
    public int SuccessfulRecords { get; init; }
    public int FailedRecords { get; init; }
}