using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;

public class GetCoursesSearchRequest : IApiRequest
{
    public string GetUrl => "api/courses/search?filter=Active&orderby=Score";
    public object Data => null!;
}
