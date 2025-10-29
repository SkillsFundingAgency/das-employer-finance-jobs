namespace SFA.DAS.Employer.Finance.Jobs;

public class ProcessFinanceCommand 
{
    public Guid JobId { get; set; }
    public DateTime QueuedAt { get; set; }
    public string? Source { get; set; }
}
