using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;
public class GetLevyDeclarationLastSubmissionDateRequest(string empRef) : IApiRequest
{
    public string GetUrl => $"api/paye-schemes/last-submission-date?empRef={Uri.EscapeDataString(empRef)}";
    public object Data => new { };
}
