using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;
using SFA.DAS.Employer.Finance.Jobs.UnitTests.Helpers;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Services;

public class WhenApplyingHmrcRequestThrottle
{
    [Test]
    public async Task Then_Combined_Hmrc_Traffic_Is_Limited_To_Six_Requests_Every_Two_Seconds()
    {
        var clock = new FakeHmrcClock(new DateTimeOffset(2026, 4, 13, 9, 0, 0, TimeSpan.Zero));
        var logger = new Mock<ILogger<HmrcRequestThrottle>>();
        var throttle = new HmrcRequestThrottle(clock, logger.Object);

        for (var index = 0; index < 6; index++)
        {
            await throttle.WaitAsync(index % 2 == 0 ? "GetLevyDeclarations" : "GetEnglishFractions");
        }

        clock.Delays.Should().BeEmpty();

        await throttle.WaitAsync("GetLastEnglishFractionUpdate");

        clock.Delays.Should().ContainSingle().Which.Should().Be(TimeSpan.FromSeconds(2));
        logger.VerifyLogContains(LogLevel.Information, "HMRC throttle reached");
    }
}
