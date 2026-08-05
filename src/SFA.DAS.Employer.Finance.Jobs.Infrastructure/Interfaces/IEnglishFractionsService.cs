using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

public interface IEnglishFractionsService
{
    Task<EnglishFractionsFetchResult> GetEnglishFractionsAsync(GetEnglishFractionsActivityInput input, CancellationToken cancellationToken = default);
}
