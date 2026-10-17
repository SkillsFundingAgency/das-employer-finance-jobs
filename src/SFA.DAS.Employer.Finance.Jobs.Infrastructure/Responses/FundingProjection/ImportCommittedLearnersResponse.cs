namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses.FundingProjection;

public sealed record ImportCommittedLearnersResponse
{
    public bool IsSuccessful { get; set; }
    public int TotalRecordsProcessed { get; set; }
    public int FailedRecordsCount { get; set; }
    public DateTime AttemptedDate { get; set; }
    public DateTime? LastSuccessfulImportDate { get; set; }
}