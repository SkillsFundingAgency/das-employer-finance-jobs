using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;

public class GetPaymentPeriodEndsRequest : IApiRequest
{
    public string GetUrl => "api/periodends";

    public object Data { get; set; }
}