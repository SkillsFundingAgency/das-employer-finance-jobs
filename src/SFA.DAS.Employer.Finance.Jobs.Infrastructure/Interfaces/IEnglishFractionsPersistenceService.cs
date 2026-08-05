using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

public interface IEnglishFractionsPersistenceService
{
    Task<EnglishFractionsPersistenceResult> PersistEnglishFractionsAsync(EnglishFractionsFetchResult input, CancellationToken cancellationToken = default);
}
