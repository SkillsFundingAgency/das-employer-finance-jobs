namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Models;

public class HmrcLevyDeclaration
{
    public string Id { get; set; } = string.Empty;
    public long SubmissionId { get; set; }
    public decimal? LevyDueYearToDate { get; set; }
    public DateTime SubmissionTime { get; set; }
    public string SubmissionType { get; set; } = string.Empty;
    public decimal LevyAllowanceForFullYear { get; set; }
    public HmrcPayrollPeriod? PayrollPeriod { get; set; }
    public bool NoPaymentForPeriod { get; set; }
    public DateTime? DateCeased { get; set; }
    public DateTime? InactiveFrom { get; set; }
    public DateTime? InactiveTo { get; set; }
}
