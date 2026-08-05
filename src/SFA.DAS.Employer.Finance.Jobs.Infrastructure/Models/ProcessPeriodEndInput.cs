namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public class ProcessPeriodEndInput
{
    public string PeriodId { get; set; }
    public DateTime AccountDataValidAt { get; set; }
    public DateTime CommitmentDataValidAt { get; set; }
}
