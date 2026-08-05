using FluentAssertions;
using NUnit.Framework;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services.HMRC;
using System.Diagnostics;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.UnitTests.Services.HMRC;

[TestFixture]
public class SlidingWindowHmrcRateLimiterTests
{
    [Test]
    public async Task WaitForAvailabilityAsync_Allows_First_Six_Requests_In_Window_Without_Delay()
    {
        var limiter = new SlidingWindowHmrcRateLimiter(6, TimeSpan.FromSeconds(2));

        var waits = new List<TimeSpan>();
        for (var i = 0; i < 6; i++)
        {
            waits.Add(await limiter.WaitForAvailabilityAsync(CancellationToken.None));
        }

        waits.Should().OnlyContain(x => x == TimeSpan.Zero);
    }

    [Test]
    public async Task WaitForAvailabilityAsync_Delays_Seventh_Request_Until_Window_Opens()
    {
        var window = TimeSpan.FromMilliseconds(50);
        var limiter = new SlidingWindowHmrcRateLimiter(6, window);

        for (var i = 0; i < 6; i++)
        {
            await limiter.WaitForAvailabilityAsync(CancellationToken.None);
        }

        var stopwatch = Stopwatch.StartNew();
        var wait = await limiter.WaitForAvailabilityAsync(CancellationToken.None);
        stopwatch.Stop();

        wait.Should().BeGreaterThan(TimeSpan.Zero);
        stopwatch.Elapsed.Should().BeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(30));
    }

    [Test]
    public async Task WaitForAvailabilityAsync_Allows_Request_After_Window_Expires()
    {
        var window = TimeSpan.FromMilliseconds(20);
        var limiter = new SlidingWindowHmrcRateLimiter(1, window);

        var firstWait = await limiter.WaitForAvailabilityAsync(CancellationToken.None);
        await Task.Delay(window + TimeSpan.FromMilliseconds(10));
        var secondWait = await limiter.WaitForAvailabilityAsync(CancellationToken.None);

        firstWait.Should().Be(TimeSpan.Zero);
        secondWait.Should().Be(TimeSpan.Zero);
    }

    [Test]
    public void Constructor_Throws_When_MaxRequestsPerWindow_Is_Not_Positive()
    {
        var act = () => new SlidingWindowHmrcRateLimiter(0, TimeSpan.FromSeconds(2));

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("maxRequestsPerWindow");
    }

    [Test]
    public void Constructor_Throws_When_Window_Is_Not_Positive()
    {
        var act = () => new SlidingWindowHmrcRateLimiter(6, TimeSpan.Zero);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("window");
    }
}
