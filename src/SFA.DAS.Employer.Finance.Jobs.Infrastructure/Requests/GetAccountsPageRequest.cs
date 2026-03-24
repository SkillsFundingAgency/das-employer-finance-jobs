using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;

public class GetAccountsPageRequest(int pageNumber, int pageSize) : IApiRequest
{
    public int PageNumber { get; } = pageNumber;
    public int PageSize { get; } = pageSize;

    public string GetUrl => $"/api/accounts?pageNumber={PageNumber}&pageSize={PageSize}";
    public object? Data => null;
}
