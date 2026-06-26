using System.Threading;
using Microsoft.Extensions.Logging;
using NServiceBus;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;
using SFA.DAS.EmployerFinance.Messages.Events;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Services;

[TestFixture]
public class WhenPublishingRefreshPaymentDataCompletedEvent
{
    private Mock<IMessageSession> _messageSessionMock;
    private Mock<ILogger<RefreshPaymentDataCompletedEventPublisher>> _loggerMock;
    private RefreshPaymentDataCompletedEventPublisher _publisher;

    [SetUp]
    public void SetUp()
    {
        _messageSessionMock = new Mock<IMessageSession>();
        _loggerMock = new Mock<ILogger<RefreshPaymentDataCompletedEventPublisher>>();
        _publisher = new RefreshPaymentDataCompletedEventPublisher(_messageSessionMock.Object, _loggerMock.Object);
    }

    [Test]
    public async Task Then_Publishes_Event_Through_NServiceBus_Message_Session()
    {
        PublishOptions publishOptions = null;
        var completedEvent = new RefreshPaymentDataCompletedEvent
        {
            AccountId = 12345,
            PeriodEnd = "2024-01",
            Created = DateTime.UtcNow,
            PaymentsProcessed = true
        };

        _messageSessionMock
            .Setup(session => session.Publish(
                completedEvent,
                It.IsAny<PublishOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback<object, PublishOptions, CancellationToken>((_, options, _) => publishOptions = options)
            .Returns(Task.CompletedTask);

        await _publisher.Publish(completedEvent, "correlation-id");

        _messageSessionMock.Verify(
            session => session.Publish(
                completedEvent,
                It.IsAny<PublishOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        publishOptions.Should().NotBeNull();
        publishOptions.GetHeaders()[Headers.CorrelationId].Should().Be("correlation-id");
    }
}
