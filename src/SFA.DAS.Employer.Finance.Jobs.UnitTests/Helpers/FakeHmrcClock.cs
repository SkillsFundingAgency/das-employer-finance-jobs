using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Helpers;

public class FakeHmrcClock(DateTimeOffset startTime) : IHmrcClock
{
    public List<TimeSpan> Delays { get; } = [];

    public DateTimeOffset UtcNow { get; private set; } = startTime;

    public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default)
    {
        Delays.Add(delay);
        UtcNow = UtcNow.Add(delay);
        return Task.CompletedTask;
    }
}
