namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public class RefreshAccountTransfersResult
{
    public int TransfersProcessed { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
