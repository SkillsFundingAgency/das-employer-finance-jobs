using Microsoft.Extensions.Logging;
using Moq;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.UnitTests.Helpers;

public static class LoggerExtensions
{
    public static void VerifyLogContains<T>(this Mock<ILogger<T>> loggerMock, string contains)
    {
        loggerMock.Verify(x =>
                x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains(contains)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce,
                $"Expected log containing '{contains}' but none was found");
    }
}
