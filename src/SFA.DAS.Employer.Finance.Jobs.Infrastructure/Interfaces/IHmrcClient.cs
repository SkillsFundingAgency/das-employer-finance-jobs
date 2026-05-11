using HMRC.ESFA.Levy.Api.Types;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

public interface IHmrcClient
{
    Task<EnglishFractionDeclarations> GetEnglishFractionsAsync(string employerReference, DateTime? fromDate, CancellationToken cancellationToken = default);
    Task<DateTime> GetLastEnglishFractionUpdateAsync(CancellationToken cancellationToken = default);
}
