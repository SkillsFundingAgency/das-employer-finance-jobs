namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public class GetEnglishFractionsActivityInput
{
    public string CorrelationId { get; set; } = string.Empty;
    public string EmployerReference { get; set; } = string.Empty;
    public DateTime? LastStoredFractionCalculatedDate { get; set; }
}
