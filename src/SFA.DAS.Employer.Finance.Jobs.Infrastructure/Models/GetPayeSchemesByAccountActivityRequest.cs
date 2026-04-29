namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public record GetPayeSchemesByAccountActivityRequest(long AccountId, string CorrelationId, string? Source = null);
