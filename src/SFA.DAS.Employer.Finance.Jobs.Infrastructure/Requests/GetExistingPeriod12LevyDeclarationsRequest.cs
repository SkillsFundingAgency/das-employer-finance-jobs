using System.Web;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Requests;

public class GetExistingPeriod12LevyDeclarationsRequest(string empRef) : IApiRequest
{
    public string GetUrl => $"api/levy-declarations/{HttpUtility.UrlEncode(empRef)}/period-12-declarations";
    public object? Data => null;
}