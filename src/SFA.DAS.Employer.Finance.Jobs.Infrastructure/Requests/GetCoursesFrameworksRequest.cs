using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;

public class GetCoursesFrameworksRequest : IApiRequest
{
    public string GetUrl => "api/courses/frameworks";
    public object Data => null!;
}
