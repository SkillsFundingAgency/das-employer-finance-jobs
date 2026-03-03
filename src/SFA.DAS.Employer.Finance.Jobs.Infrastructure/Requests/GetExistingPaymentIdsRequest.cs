using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;

public class GetExistingPaymentIdsRequest(long accountId) : IGetApiRequest
{
  public string GetUrl =>BuildGetUrl();

    public object Data => throw new NotImplementedException();

    private string BuildGetUrl()
    {
        var url = $"api/accounts/{accountId}/payments/ids";
        return url;
    }
}