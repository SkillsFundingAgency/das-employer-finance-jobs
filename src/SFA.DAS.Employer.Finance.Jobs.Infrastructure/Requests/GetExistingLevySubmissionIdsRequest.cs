using System.Web;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Requests;

public class GetExistingLevySubmissionIdsRequest(string empRef) : IApiRequest
{
    public string GetUrl => $"api/levy-declarations/{HttpUtility.UrlEncode(empRef)}/submission-ids";
    public object? Data => null;
}