namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public class EnglishFractionsFetchResult
{
    public string CorrelationId { get; set; } = string.Empty;
    public string EmployerReference { get; set; } = string.Empty;
    public bool UpdateRequired { get; set; }
    public DateTime HmrcLatestUpdateDate { get; set; }
    public DateTime? RequestedFrom { get; set; }
    public List<EnglishFraction> Fractions { get; set; } = [];
}
