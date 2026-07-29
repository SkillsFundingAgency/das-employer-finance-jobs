using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;

public class PostTransfersToStagingRequest(object data) : IApiRequest
{
    public string GetUrl => "api/transfers/staging";
    public object Data { get; set; } = data;
}
