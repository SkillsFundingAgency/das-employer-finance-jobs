using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;

public class GetAccountByIdRequest(long accountId) : IApiRequest
{
    public string GetUrl => $"api/accounts/{accountId}";
    public object Data { get; set; }
}
