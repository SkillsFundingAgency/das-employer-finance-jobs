using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public class GetAccountsRequest : IApiRequest
{
    public int Page { get; set; }
    public int PageSize { get; set; }

    public string GetUrl => throw new NotImplementedException();

    public object Data => throw new NotImplementedException();

    public Guid CorrelationId { get; set; }
}
