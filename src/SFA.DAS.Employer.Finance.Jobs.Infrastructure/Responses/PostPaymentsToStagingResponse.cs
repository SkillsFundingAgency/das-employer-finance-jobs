namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;

public class PostPaymentsToStagingResponse
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public int InsertedCount { get; set; }
    public List<Guid>? PaymentIds { get; set; }
}