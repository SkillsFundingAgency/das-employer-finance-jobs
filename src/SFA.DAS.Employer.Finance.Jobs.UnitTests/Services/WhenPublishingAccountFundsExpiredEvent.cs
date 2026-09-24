using System.Threading;
using Microsoft.Extensions.Logging;
using NServiceBus;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;
using SFA.DAS.Employer.Finance.Jobs.UnitTests.Helpers;
using SFA.DAS.EmployerFinance.Messages.Events;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Services;

[TestFixture]
public class WhenPublishingAccountFundsExpiredEvent
{
    [Test]
    public async Task Then_The_Event_Is_Published_With_Correlation_And_A_Stable_Message_Id()
    {
        var messageSession = new Mock<IMessageSession>();
        var logger = new Mock<ILogger<AccountFundsExpiredEventPublisher>>();
        var publisher = new AccountFundsExpiredEventPublisher(messageSession.Object, logger.Object);
        var accountFundsExpiredEvent = new AccountFundsExpiredEvent
        {
            AccountId = 12345,
            Created = new DateTime(2026, 9, 3, 10, 30, 0, DateTimeKind.Utc)
        };
        PublishOptions capturedOptions = null!;

        messageSession
            .Setup(session => session.Publish(
                accountFundsExpiredEvent,
                It.IsAny<PublishOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<object, PublishOptions, CancellationToken>((_, options, _) => capturedOptions = options)
            .Returns(Task.CompletedTask);

        await publisher.Publish(
            accountFundsExpiredEvent,
            "correlation-id",
            "AccountFundsExpiredEvent-correlation-id-12345");

        messageSession.Verify(
            session => session.Publish(
                accountFundsExpiredEvent,
                It.IsAny<PublishOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        capturedOptions.GetHeaders()[Headers.CorrelationId].Should().Be("correlation-id");
        capturedOptions.GetMessageId().Should().Be("AccountFundsExpiredEvent-correlation-id-12345");
        logger.VerifyLogContains(LogLevel.Information, "AccountId 12345");
        logger.VerifyLogContains(LogLevel.Information, "correlation-id");
    }
}
