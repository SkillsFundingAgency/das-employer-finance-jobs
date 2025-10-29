namespace DAS.Employer.Finance.Jobs;

public class ProcessFinanceCommand : ICommand
{
    public Guid JobId { get; set; }
    public DateTime QueuedAt { get; set; }
    public string? Source { get; set; }
}
