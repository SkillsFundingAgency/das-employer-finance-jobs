using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;

public class PostTransactionLinesToStagingRequest<TData> : IPostApiRequest<TData>
{
    public string PostUrl => "api/transactions/payments/staging";
    public TData Data { get; set; }
    public PostTransactionLinesToStagingRequest(TData data)
    {
        Data = data;
    }
}
