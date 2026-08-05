using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;

public class PostTransactionLinesToStagingRequest : IApiRequest
{
    public string GetUrl => "api/transaction-lines/staging";
    public object Data { get; set; }

    public PostTransactionLinesToStagingRequest(object data)
    {
        Data = data;
    }
}
