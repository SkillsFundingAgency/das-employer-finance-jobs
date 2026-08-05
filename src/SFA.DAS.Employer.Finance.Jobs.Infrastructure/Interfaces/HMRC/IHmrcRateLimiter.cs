namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces.HMRC;

public interface IHmrcRateLimiter
{
    Task<TimeSpan> WaitForAvailabilityAsync(CancellationToken cancellationToken);
}
