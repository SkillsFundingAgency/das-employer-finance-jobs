using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;

public class GetApprenticeshipRequest(long apprenticeshipId) : IApiRequest
{
    public string GetUrl => $"api/apprenticeships/{apprenticeshipId}";
    public object Data => null!;
}
