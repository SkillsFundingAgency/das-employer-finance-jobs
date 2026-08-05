using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;

public class GetExistingPaymentIdsRequest(long accountId) : IApiRequest
{
    public string GetUrl => BuildGetUrl();

    public object Data { get; set; }

    public GetAccountPaymentsRequest Payload
    {
        set => Data = value;
    }
    private string BuildGetUrl()
    {
        var url = $"api/accounts/{accountId}/payments/ids";
        return url;
    }
}
