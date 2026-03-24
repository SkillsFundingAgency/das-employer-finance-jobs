using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests
{
    public class GetLevyAccountsRequest : IApiRequest
    {
        public string GetUrl => "/api/accounts/levy";
        public object? Data => null;
    }
}
