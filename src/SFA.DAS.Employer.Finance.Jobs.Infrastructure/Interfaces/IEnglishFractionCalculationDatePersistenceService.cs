using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

public interface IEnglishFractionCalculationDatePersistenceService
{
    Task<EnglishFractionCalculationDatePersistenceResult> PersistCalculationDateAsync(
        EnglishFractionsFetchResult input,
        CancellationToken cancellationToken = default);
}
