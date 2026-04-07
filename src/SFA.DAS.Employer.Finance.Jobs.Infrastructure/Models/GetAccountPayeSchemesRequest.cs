using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public class GetAccountPayeSchemesRequest : IApiRequest
{
    public long AccountId { get; set; }
    public string Source { get; set; } = "government-gateway";

    public string GetUrl => $"api/accounts/{AccountId}/paye-schemes?source={Source}";

    public object? Data { get; set; }

    public Guid CorrelationId { get; set; }
}
