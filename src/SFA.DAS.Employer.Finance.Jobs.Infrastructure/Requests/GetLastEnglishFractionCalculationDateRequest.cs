using System.Web;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;

public class GetLastEnglishFractionCalculationDateRequest(string empRef) : IApiRequest
{
    public string GetUrl => $"api/english-fraction-calculation-date/{HttpUtility.UrlEncode(empRef)}";
    public object? Data => null;
}