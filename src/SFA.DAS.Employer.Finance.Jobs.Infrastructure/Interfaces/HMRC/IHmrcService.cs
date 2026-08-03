using HMRC.ESFA.Levy.Api.Types;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces.HMRC;

public interface IHmrcService
{
    Task<LevyDeclarations> GetLevyDeclarations(string empRef, DateTime? fromDate, string correlationId, CancellationToken cancellationToken);
}
