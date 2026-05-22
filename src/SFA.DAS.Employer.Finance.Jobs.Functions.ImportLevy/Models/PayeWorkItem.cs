namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Models;

public record PayeWorkItem(long AccountId, string EmpRef, DateTime? LastSubmissionDate = null);
