using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;

public class GetAccountTransfersRequest(string periodEnd, long receiverAccountId, int page = 1) : IApiRequest
{
    public string GetUrl => $"api/transfers?page={page}&periodId={periodEnd}&receiverAccountId={receiverAccountId}";
    public object Data { get; set; } = null!;
}
