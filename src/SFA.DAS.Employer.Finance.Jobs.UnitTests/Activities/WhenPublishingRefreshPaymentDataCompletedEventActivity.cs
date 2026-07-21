using System.Threading;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Functions.Activities;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.EmployerFinance.Messages.Events;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Activities;

[TestFixture]
public class WhenPublishingRefreshPaymentDataCompletedEventActivity
{
    private Mock<ILogger<RefreshPaymentDataCompletedEventActivities>> _loggerMock;
    private Mock<IRefreshPaymentDataCompletedEventPublisher> _publisherMock;
    private RefreshPaymentDataCompletedEventActivities _activity;

    [SetUp]
    public void SetUp()
    {
        _loggerMock = new Mock<ILogger<RefreshPaymentDataCompletedEventActivities>>();
        _publisherMock = new Mock<IRefreshPaymentDataCompletedEventPublisher>();
        _activity = new RefreshPaymentDataCompletedEventActivities(_loggerMock.Object, _publisherMock.Object);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task Then_Publishes_Refresh_Payment_Data_Completed_Event(bool paymentsProcessed)
    {
        var createdBefore = DateTime.UtcNow;
        RefreshPaymentDataCompletedEvent publishedEvent = null;
        var input = new PublishRefreshPaymentDataCompletedEventInput
        {
            AccountId = 12345,
            PeriodEnd = "2024-01",
            PaymentsProcessed = paymentsProcessed,
            CorrelationId = "correlation-id"
        };

        _publisherMock
            .Setup(publisher => publisher.Publish(
                It.IsAny<RefreshPaymentDataCompletedEvent>(),
                input.CorrelationId,
                It.IsAny<CancellationToken>()))
            .Callback<RefreshPaymentDataCompletedEvent, string, CancellationToken>((completedEvent, _, _) => publishedEvent = completedEvent)
            .Returns(Task.CompletedTask);

        var result = await _activity.PublishRefreshPaymentDataCompletedEventActivity(input);
        var createdAfter = DateTime.UtcNow;

        result.Status.Should().Be("Succeeded");
        publishedEvent.Should().NotBeNull();
        publishedEvent.AccountId.Should().Be(input.AccountId);
        publishedEvent.PeriodEnd.Should().Be(input.PeriodEnd);
        publishedEvent.PaymentsProcessed.Should().Be(paymentsProcessed);
        publishedEvent.Created.Should().BeOnOrAfter(createdBefore);
        publishedEvent.Created.Should().BeOnOrBefore(createdAfter);
    }

    [Test]
    public void Then_Logs_And_Throws_When_Publishing_Fails()
    {
        var input = new PublishRefreshPaymentDataCompletedEventInput
        {
            AccountId = 12345,
            PeriodEnd = "2024-01",
            PaymentsProcessed = true,
            CorrelationId = "correlation-id"
        };

        _publisherMock
            .Setup(publisher => publisher.Publish(
                It.IsAny<RefreshPaymentDataCompletedEvent>(),
                input.CorrelationId,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Service Bus publish failed"));

        Assert.ThrowsAsync<InvalidOperationException>(() => _activity.PublishRefreshPaymentDataCompletedEventActivity(input));
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("Failed to publish RefreshPaymentDataCompletedEvent")),
                It.Is<InvalidOperationException>(exception => exception.Message == "Service Bus publish failed"),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
}
