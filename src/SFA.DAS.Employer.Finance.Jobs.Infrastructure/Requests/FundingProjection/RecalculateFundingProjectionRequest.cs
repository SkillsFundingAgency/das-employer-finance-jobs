using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests.FundingProjection;

internal class RecalculateFundingProjectionRequest : IApiRequest
{
    public string GetUrl => "funding-projection/re-calculate";
    public object? Data { get; } = null;
}