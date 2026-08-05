namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public class EnglishFractionsPersistenceResult
{
    public string CorrelationId { get; set; } = string.Empty;
    public string EmployerReference { get; set; } = string.Empty;
    public bool UpdateRequired { get; set; }
    public int Stored { get; set; }
    public int Ignored { get; set; }
    public bool Skipped { get; set; }
}
