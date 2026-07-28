namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public class PaymentMetadataStaging
{
    public string? ProviderName { get; set; }
    public long? StandardCode { get; set; }
    public int? FrameworkCode { get; set; }
    public int? ProgrammeType { get; set; }
    public int? PathwayCode { get; set; }
    public string? CourseCode { get; set; }
    public string LearningType { get; set; } = "Apprenticeship";
    public long? CohortId { get; set; }
    public string? PathwayName { get; set; }
    public string? ApprenticeshipCourseName { get; set; }
    public DateTime? ApprenticeshipCourseStartDate { get; set; }
    public int? ApprenticeshipCourseLevel { get; set; }
    public string? ApprenticeName { get; set; }
    public string? ApprenticeNINumber { get; set; }
    public bool IsHistoricProviderName { get; set; }
    public string CreatedBy { get; set; } = "EmployerFinanceJobs";
    public Guid? CorrelationId { get; set; }
    public Guid PaymentId { get; set; }
}
