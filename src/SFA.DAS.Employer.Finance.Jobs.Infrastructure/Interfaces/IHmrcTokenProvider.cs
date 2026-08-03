namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

public interface IHmrcTokenProvider
{
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}
