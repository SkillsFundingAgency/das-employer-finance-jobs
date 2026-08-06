using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;

public class PostPaymentsToStagingRequest : IApiRequest
{
    public string GetUrl => "api/payments/staging";
    public object Data { get; set; }

    public PostPaymentsToStagingRequest(object data)
    {
        Data = data;
    }
}
