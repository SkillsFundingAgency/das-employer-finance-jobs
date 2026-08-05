namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;

public class PostTransactionLinesToStagingResponse
{
    public bool IsSuccess { get; set; }
    public int InsertedCount { get; set; }
    public string? Message { get; set; }
}
