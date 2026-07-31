namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public record ImportLevyActivityRequest(string EmpRef, DateTime? FromDate, string CorrelationId);
