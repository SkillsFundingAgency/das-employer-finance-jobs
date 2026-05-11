namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public class EnglishFractionCalculationDatePersistenceResult
{
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime DateCalculated { get; set; }
    public bool UpdateRequired { get; set; }
    public bool Persisted { get; set; }
    public bool Skipped { get; set; }
    public bool AlreadyPersistedForRunDate { get; set; }
}
