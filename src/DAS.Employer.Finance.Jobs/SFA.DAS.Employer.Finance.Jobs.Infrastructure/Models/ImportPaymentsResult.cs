namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public class ImportPaymentsResult
{
    public bool Success { get; set; }
    public int NewPeriodEndsCount { get; set; }
    public int TotalPeriodEndsCount { get; set; }
    public string ErrorMessage { get; set; }
    public string CorrelationId { get; set; }
}
