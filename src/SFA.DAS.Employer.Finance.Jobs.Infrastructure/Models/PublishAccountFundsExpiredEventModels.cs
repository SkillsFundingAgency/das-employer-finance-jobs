namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public class PublishAccountFundsExpiredEventInput
{
    public long AccountId { get; set; }
    public DateTime Created { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string MessageId { get; set; } = string.Empty;
}

public class PublishAccountFundsExpiredEventResult
{
    public long AccountId { get; set; }
    public bool Published { get; set; }
    public string? ErrorMessage { get; set; }
}
