using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NServiceBus;
using NUnit.Framework;
using SFA.DAS.Employer.Finance.Jobs.Handler;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Handler;
public class WhenCallingProcessFinanceCommandHandlerTests
{
    private Mock<ILogger<ProcessFinanceCommandHandler>> _loggerMock;
    private ProcessFinanceCommandHandler _handler;
    private Mock<IMessageHandlerContext> _contextMock;

    [SetUp]
    public void SetUp()
    {
        _loggerMock = new Mock<ILogger<ProcessFinanceCommandHandler>>();
        _handler = new ProcessFinanceCommandHandler(_loggerMock.Object);
        _contextMock = new Mock<IMessageHandlerContext>();
    }

    [Test]
    public async Task Then_Handle_LogsInformationWithCorrectParameters()
    {
        // Arrange
        var command = new ProcessFinanceCommand
        {
            JobId = Guid.NewGuid(),
            QueuedAt = DateTime.UtcNow,
            Source = "UnitTest"
        };

        // Act
        var task = _handler.Handle(command, _contextMock.Object);

        // Assert
        await task;
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => StateContainsOriginalFormat(v, "Received ProcessFinanceCommand. JobId: {JobId}, QueuedAt: {QueuedAt}, Source: {Source}")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        task.IsCompletedSuccessfully.Should().BeTrue();
    }

    private static bool StateContainsOriginalFormat(object state, string expectedFormat)
    {
        if (state is IEnumerable<KeyValuePair<string, object>> kvps)
        {
            foreach (var kv in kvps)
            {
                if (kv.Key == "{OriginalFormat}" || kv.Key == "OriginalFormat")
                {
                    return string.Equals(kv.Value?.ToString(), expectedFormat, StringComparison.Ordinal);
                }
            }
        }       
        return state?.ToString()?.Contains("Received ProcessFinanceCommand.") ?? false;
    }
}