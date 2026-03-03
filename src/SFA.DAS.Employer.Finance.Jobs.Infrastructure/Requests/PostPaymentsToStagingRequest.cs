using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;

public class PostPaymentsToStagingRequest<TData> : IPostApiRequest<TData>
{
    public string PostUrl => "api/payments/staging";
    public TData Data { get; set; }

    public PostPaymentsToStagingRequest(TData data)
    {
        Data = data;
    }
}