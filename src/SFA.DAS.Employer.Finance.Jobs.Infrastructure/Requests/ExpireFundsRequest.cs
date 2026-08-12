using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;

public class ExpireFundsRequest(long accountId, ExpireFundsRequestData data) : IApiRequest
{
    public string GetUrl => $"api/accounts/{accountId}/expire-funds";
    public object Data { get; } = data;
}

public class ExpireFundsRequestData
{
    public string CorrelationId { get; set; } = string.Empty;
}
