using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.InnerAPI.Requests;

public class GetFinancePeriodEndsRequest : IGetApiRequest
{
  public string GetUrl => "api/period-ends"; 
}