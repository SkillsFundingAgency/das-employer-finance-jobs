using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

public interface IExpireFundsService
{
    Task<ExpireFundsResponse> ExpireFundsAsync(long accountId, string correlationId);
}
