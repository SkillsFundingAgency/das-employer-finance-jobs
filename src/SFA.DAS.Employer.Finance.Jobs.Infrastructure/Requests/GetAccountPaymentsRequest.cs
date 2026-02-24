using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;

public class GetAccountPaymentsRequest(string PeriodEnd, long accountId) : IGetApiRequest
{
    public string GetUrl => BuildGetUrl();

    private string BuildGetUrl()
    {
        var url = $"api/accounts/{accountId}/payments/{PeriodEnd} ";
        return url;
    }
}