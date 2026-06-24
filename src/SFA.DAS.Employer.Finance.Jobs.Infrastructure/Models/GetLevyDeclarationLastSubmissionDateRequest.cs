using System.Web;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;
public class GetLevyDeclarationLastSubmissionDateRequest(string empRef) : IApiRequest
{
    public string GetUrl => $"api/paye-schemes/{HttpUtility.UrlEncode(empRef)}/last-submission-date";
    public object Data => new { };
}
