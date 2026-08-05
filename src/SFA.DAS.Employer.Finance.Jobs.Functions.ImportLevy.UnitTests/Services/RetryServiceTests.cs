using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Services;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.UnitTests.Services;

[TestFixture]
public class RetryServiceTests
{
    private Mock<IRetryDelay> _retryDelay = null!;
    private RetryService _retryService = null!;

    [SetUp]
    public void SetUp()
    {
        _retryDelay = new Mock<IRetryDelay>();
        _retryDelay
            .Setup(x => x.DelayAsync(It.IsAny<TimeSpan>()))
            .Returns(Task.CompletedTask);

        _retryService = new RetryService(
            Mock.Of<ILogger<RetryService>>(),
            _retryDelay.Object);
    }

    [Test]
    public async Task ExecuteAsync_ReturnsResult_WhenFirstAttemptSucceeds()
    {
        var result = await _retryService.ExecuteAsync(
            () => Task.FromResult("success"),
            "corr-123",
            "Finance API");

        result.Should().Be("success");
        _retryDelay.Verify(x => x.DelayAsync(It.IsAny<TimeSpan>()), Times.Never);
    }

    [Test]
    public async Task ExecuteAsync_RetriesWithExponentialBackoff_AndReturnsResult()
    {
        var attempts = 0;

        var result = await _retryService.ExecuteAsync(
            () =>
            {
                attempts++;

                if (attempts < 3)
                {
                    throw new InvalidOperationException("temporary failure");
                }

                return Task.FromResult("success");
            },
            "corr-123",
            "Finance API");

        result.Should().Be("success");
        attempts.Should().Be(3);
        _retryDelay.Verify(x => x.DelayAsync(TimeSpan.FromSeconds(2)), Times.Once);
        _retryDelay.Verify(x => x.DelayAsync(TimeSpan.FromSeconds(4)), Times.Once);
    }

    [Test]
    public async Task ExecuteAsync_Throws_WhenRetriesAreExhausted()
    {
        var attempts = 0;

        Func<Task> act = async () => await _retryService.ExecuteAsync<string>(
            () =>
            {
                attempts++;
                throw new InvalidOperationException("still failing");
            },
            "corr-123",
            "Finance API");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("still failing");

        attempts.Should().Be(3);
        _retryDelay.Verify(x => x.DelayAsync(TimeSpan.FromSeconds(2)), Times.Once);
        _retryDelay.Verify(x => x.DelayAsync(TimeSpan.FromSeconds(4)), Times.Once);
    }

    [Test]
    public async Task ExecuteAsync_DoesNotRetry_WhenPredicateRejectsException()
    {
        var attempts = 0;

        Func<Task> act = async () => await _retryService.ExecuteAsync<string>(
            () =>
            {
                attempts++;
                throw new InvalidOperationException("permanent failure");
            },
            "corr-123",
            "Finance API",
            _ => false);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("permanent failure");

        attempts.Should().Be(1);
        _retryDelay.Verify(x => x.DelayAsync(It.IsAny<TimeSpan>()), Times.Never);
    }

    [Test]
    public async Task ExecuteAsync_Retries_WhenPredicateAcceptsException()
    {
        var attempts = 0;

        var result = await _retryService.ExecuteAsync(
            () =>
            {
                attempts++;
                return attempts == 1
                    ? throw new InvalidOperationException("temporary failure")
                    : Task.FromResult("success");
            },
            "corr-123",
            "Finance API",
            _ => true);

        result.Should().Be("success");
        attempts.Should().Be(2);
        _retryDelay.Verify(x => x.DelayAsync(TimeSpan.FromSeconds(2)), Times.Once);
    }
}
