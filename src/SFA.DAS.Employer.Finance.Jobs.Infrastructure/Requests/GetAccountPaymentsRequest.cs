using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;


public class GetAccountPaymentsRequest(string PeriodEnd, long accountId, int page = 1) : IApiRequest
{
    public string GetUrl => BuildGetUrl();

    public object Data { get; set; }

    public GetAccountPaymentsRequest Payload
    {
        set => Data = value;
    }
    private string BuildGetUrl()
    {
        var url = $"api/payments?page={page}&periodId={PeriodEnd}&employerAccountId={accountId}";
        return url;
    }
}
