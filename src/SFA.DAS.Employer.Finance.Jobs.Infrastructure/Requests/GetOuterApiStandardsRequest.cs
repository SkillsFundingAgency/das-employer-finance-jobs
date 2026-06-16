using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;

public class GetOuterApiStandardsRequest : IApiRequest
{
    public string GetUrl => "trainingCourses/standards";
    public object Data => null!;
}
