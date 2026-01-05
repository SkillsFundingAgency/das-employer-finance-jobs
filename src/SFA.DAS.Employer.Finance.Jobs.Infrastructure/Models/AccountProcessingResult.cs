namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public class AccountProcessingResult
{
    public long AccountId { get; set; }
    public bool Success { get; set; }
    public int PaymentsProcessed { get; set; }
    public int TransfersProcessed { get; set; }
}