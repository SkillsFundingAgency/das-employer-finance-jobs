namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Models;

public class ExistingPeriod12LevyDeclarationResult
{
    public string Id { get; set; } = string.Empty;
    public decimal? LevyDueYtd { get; set; }
    public DateTime SubmissionDate { get; set; }
    public string PayrollYear { get; set; } = string.Empty;
    public short? PayrollMonth { get; set; }
    public long SubmissionId { get; set; }
}
