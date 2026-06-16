using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;

public class GetOuterApiFrameworksRequest : IApiRequest
{
    public string GetUrl => "trainingCourses/frameworks";
    public object Data => null!;
}
