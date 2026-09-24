namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses.FundingProjection;

public sealed record ImportCommittedLearnersResponse
{
    public int TotalRecords { get; init; }
    public int FailedRecords { get; init; }
    public int BatchesProcessed { get; init; }
}