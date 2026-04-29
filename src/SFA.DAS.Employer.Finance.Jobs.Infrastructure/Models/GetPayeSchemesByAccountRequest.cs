using System.Web;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;
public class GetPayeSchemesByAccountRequest(long accountId, string? source = null) : IApiRequest
{
    public string GetUrl => BuildGetUrl();
    public object Data => new { };

    private string BuildGetUrl()
    {
        var url = $"api/accounts/{accountId}/paye-schemes";
        if (string.IsNullOrWhiteSpace(source))
        {
            return url;
        }

        var encodedSource = HttpUtility.UrlEncode(source);
        return $"{url}?source={encodedSource}";
    }
}
