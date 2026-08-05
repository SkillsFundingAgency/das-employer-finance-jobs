using Microsoft.Extensions.Logging;
using Moq;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Helpers;

public static class LoggerExtensions
{
    public static void VerifyLogContains<T>(this Mock<ILogger<T>> loggerMock, LogLevel logLevel, string contains)
    {
        loggerMock.Verify(x =>
                x.Log(
                    logLevel,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains(contains)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce,
                $"Expected {logLevel} log containing '{contains}' but none was found");
    }
}
