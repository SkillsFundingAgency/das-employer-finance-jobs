namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;

public class PostTransferStagedToOperationalResponse
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public int ProcessedCount { get; set; }
}
