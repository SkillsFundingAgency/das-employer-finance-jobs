using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;

public class GetPaymentPeriodEndsRequest : IGetApiRequest
{
  public string GetUrl => "api/periodends";  
}