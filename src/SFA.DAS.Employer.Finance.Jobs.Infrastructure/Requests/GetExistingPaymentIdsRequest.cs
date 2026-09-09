using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;

public class GetExistingPaymentIdsRequest(long accountId, int pageNumber = 1, int pageSize = 10000) : IApiRequest
{
    public string GetUrl => $"api/accounts/{accountId}/payments/ids?pageNumber={pageNumber}&pageSize={pageSize}";

    public object Data { get; set; }

    public GetAccountPaymentsRequest Payload
    {
        set => Data = value;
    }
}
