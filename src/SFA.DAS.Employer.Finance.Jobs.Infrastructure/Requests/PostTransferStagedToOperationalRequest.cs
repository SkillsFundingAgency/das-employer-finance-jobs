using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;

public class PostTransferStagedToOperationalRequest(object data) : IApiRequest
{
    public string GetUrl => "api/transfers/staged-to-operational";
    public object Data { get; set; } = data;
}
