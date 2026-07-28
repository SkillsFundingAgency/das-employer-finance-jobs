using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

public interface ICoursesApiClient
{
    Task<StandardsResponse?> GetStandards();
    Task<FrameworksResponse?> GetFrameworks();
}
