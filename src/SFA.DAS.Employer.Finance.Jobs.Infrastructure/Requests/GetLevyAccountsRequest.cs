using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests
{
    public class GetLevyAccountsRequest : IGetApiRequest
    {
        public string GetUrl => "/api/accounts/levy";
    }
}
