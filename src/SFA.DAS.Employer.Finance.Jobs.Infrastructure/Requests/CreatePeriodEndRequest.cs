using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;

public class CreatePeriodEndRequest : IApiRequest
{
    public string GetUrl => "api/period-ends";

    public object Data { get; set; }
}
