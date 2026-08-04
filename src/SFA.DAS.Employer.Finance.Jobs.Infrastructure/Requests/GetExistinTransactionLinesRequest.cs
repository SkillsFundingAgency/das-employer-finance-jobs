using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;
public class GetExistinTransactionLinesRequest(string hashedAccountId, string periodEnd) : IApiRequest
{
    public string GetUrl => BuildGetUrl();

    public object Data { get; set; }

    private string BuildGetUrl()
    {
        var url = $"api/accounts/{hashedAccountId}/transactions?transactionType=3&periodEnd={periodEnd}";
        return url;
    }
}