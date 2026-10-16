using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests.FundingProjection;

public sealed record PostJobRequest(Guid JobId, PostJobRequestData Payload) : IApiRequest
{
    public string GetUrl => $"/api/jobs/{JobId}";
    public object? Data { get; } = Payload;
}

public sealed record PostJobRequestData
{
    public required long AccountId { get; set; }
    public string JobName { get; set; } = "ImportLearnersCost";
    public required string JobState { get; set; }
    public required DateTime LastSuccessfulImportDate { get; set; }
    public required DateTime LastAttemptedDate { get; set; }
    public required bool LastAttemptSuccessful { get; set; }
    public required int TotalRecordsLastRun { get; set; }
    public required int FailedRecordsLastRun { get; set; }
}