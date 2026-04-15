namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

public interface IHmrcClock
{
    DateTimeOffset UtcNow { get; }
    Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default);
}
