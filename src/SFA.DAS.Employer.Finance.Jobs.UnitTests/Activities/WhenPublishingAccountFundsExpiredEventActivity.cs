using System.Threading;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Functions.ExpireFunds.Activities;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.UnitTests.Helpers;
using SFA.DAS.EmployerFinance.Messages.Events;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Activities;

[TestFixture]
public class WhenPublishingAccountFundsExpiredEventActivity
{
    private Mock<IAccountFundsExpiredEventPublisher> _publisher = null!;
    private Mock<ILogger<AccountFundsExpiredEventActivities>> _logger = null!;
    private AccountFundsExpiredEventActivities _activity = null!;

    [SetUp]
    public void SetUp()
    {
        _publisher = new Mock<IAccountFundsExpiredEventPublisher>();
        _logger = new Mock<ILogger<AccountFundsExpiredEventActivities>>();
        _activity = new AccountFundsExpiredEventActivities(_publisher.Object, _logger.Object);
    }

    [Test]
    public async Task Then_The_Existing_Event_Contract_Is_Published_With_The_Supplied_Utc_Creation_Date()
    {
        var created = new DateTime(2026, 9, 3, 10, 30, 0, DateTimeKind.Utc);
        var input = new PublishAccountFundsExpiredEventInput
        {
            AccountId = 12345,
            CorrelationId = "correlation-id",
            Created = created,
            MessageId = "message-id"
        };
        AccountFundsExpiredEvent publishedEvent = null!;

        _publisher
            .Setup(publisher => publisher.Publish(
                It.IsAny<AccountFundsExpiredEvent>(),
                input.CorrelationId,
                input.MessageId,
                It.IsAny<CancellationToken>()))
            .Callback<AccountFundsExpiredEvent, string, string, CancellationToken>(
                (accountFundsExpiredEvent, _, _, _) => publishedEvent = accountFundsExpiredEvent)
            .Returns(Task.CompletedTask);

        var result = await _activity.PublishAccountFundsExpiredEventActivity(input);

        result.AccountId.Should().Be(input.AccountId);
        result.Published.Should().BeTrue();
        publishedEvent.AccountId.Should().Be(input.AccountId);
        publishedEvent.Created.Should().Be(created);
        publishedEvent.Created.Kind.Should().Be(DateTimeKind.Utc);
        _logger.VerifyLogContains(LogLevel.Information, "Published AccountFundsExpiredEvent");
        _logger.VerifyLogContains(LogLevel.Information, input.CorrelationId);
    }

    [Test]
    public void Then_A_Transient_Publication_Failure_Is_Logged_And_Propagated_For_Durable_Retry()
    {
        var input = new PublishAccountFundsExpiredEventInput
        {
            AccountId = 12345,
            CorrelationId = "correlation-id",
            Created = DateTime.UtcNow,
            MessageId = "message-id"
        };
        var expectedException = new TimeoutException("Service Bus unavailable");

        _publisher
            .Setup(publisher => publisher.Publish(
                It.IsAny<AccountFundsExpiredEvent>(),
                input.CorrelationId,
                input.MessageId,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        var exception = Assert.ThrowsAsync<TimeoutException>(() =>
            _activity.PublishAccountFundsExpiredEventActivity(input));

        exception.Should().BeSameAs(expectedException);
        _logger.VerifyLogContains(LogLevel.Warning, "transient error");
        _logger.VerifyLogContains(LogLevel.Warning, input.CorrelationId);
    }

    [Test]
    public async Task Then_A_NonTransient_Publication_Failure_Is_Returned_Without_Requesting_A_Retry()
    {
        var input = new PublishAccountFundsExpiredEventInput
        {
            AccountId = 12345,
            CorrelationId = "correlation-id",
            Created = DateTime.UtcNow,
            MessageId = "message-id"
        };

        _publisher
            .Setup(publisher => publisher.Publish(
                It.IsAny<AccountFundsExpiredEvent>(),
                input.CorrelationId,
                input.MessageId,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Invalid Service Bus configuration"));

        var result = await _activity.PublishAccountFundsExpiredEventActivity(input);

        result.AccountId.Should().Be(input.AccountId);
        result.Published.Should().BeFalse();
        result.ErrorMessage.Should().Be("Invalid Service Bus configuration");
        _logger.VerifyLogContains(LogLevel.Error, "Failed to publish AccountFundsExpiredEvent");
        _logger.VerifyLogContains(LogLevel.Error, input.CorrelationId);
    }

    [Test]
    public void Then_A_Missing_Input_Is_Rejected()
    {
        Assert.ThrowsAsync<ArgumentNullException>(() =>
            _activity.PublishAccountFundsExpiredEventActivity(null!));
    }
}
