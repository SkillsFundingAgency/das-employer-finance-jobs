using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;

public class GetExistinTransactionLinesRequest(long accountId, string periodEnd) : IGetApiRequest
{
    public string GetUrl => BuildGetUrl();

    public object Data => throw new NotImplementedException();

    private string BuildGetUrl()
    {
        var url = $"api/accounts/{accountId}/transactions?transactionType=3&periodEnd={periodEnd}";
        return url;
    }
}