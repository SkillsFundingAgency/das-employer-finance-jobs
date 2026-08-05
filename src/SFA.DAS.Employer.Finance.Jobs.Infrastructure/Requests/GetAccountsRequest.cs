using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;

public class GetAccountsRequest : IApiRequest
{
    public int Page { get; set; }
    public int PageSize { get; set; }

    public string GetUrl => $"api/accounts?pageNumber={Page}&pageSize={PageSize}";

    public object? Data { get; set; }

    public string CorrelationId { get; set; }
}
