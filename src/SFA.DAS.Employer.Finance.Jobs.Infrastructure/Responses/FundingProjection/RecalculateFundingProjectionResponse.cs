namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses.FundingProjection;

public sealed record RecalculateFundingProjectionResponse
{
    public int TotalRecordsProcessed { get; init; }
    public int TotalRecordsUpdated { get; init; }
    public int TotalRecordsInserted { get; init; }
}