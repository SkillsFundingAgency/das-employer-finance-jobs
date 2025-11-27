using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Moq;
using NServiceBus;
using NUnit.Framework;
using SFA.DAS.Employer.Finance.Jobs.Functions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Functions
{
    public class WhenRunningEmployerFinanceSendJobFunction
    {
    private Mock<IFunctionEndpoint> _mockFunctionEndpoint;
    private Mock<ILogger<EmployerFinanceSendJobFunction>> _mockLogger;
    private EmployerFinanceSendJobFunction _function;
    private Mock<FunctionContext> _mockFunctionContext;

    [SetUp]
    public void SetUp()
    {
        _mockFunctionEndpoint = new Mock<IFunctionEndpoint>();
        _mockLogger = new Mock<ILogger<EmployerFinanceSendJobFunction>>();
        _mockFunctionContext = new Mock<FunctionContext>();

        _function = new EmployerFinanceSendJobFunction(_mockFunctionEndpoint.Object, _mockLogger.Object);
    }

    [Test]
    public async Task Run_When_Called_Sends_5_Messages_With_Expected_Properties()
    {
        // Arrange
        var captured = new List<FinanceJobsQueueItem>();

        _mockFunctionEndpoint
            .Setup(x => x.Send(
                It.IsAny<FinanceJobsQueueItem>(), 
                It.IsAny<SendOptions>(), 
                It.IsAny<FunctionContext>(),
                It.IsAny<CancellationToken>()))
            .Callback<object, SendOptions, FunctionContext, CancellationToken>((msg, options, context, ct) => 
            {
                if (msg is FinanceJobsQueueItem item)
                {
                    captured.Add(item);
                }
            })
            .Returns(Task.CompletedTask)
            .Verifiable(Times.Exactly(5));

        // Act
        await _function.Run(null!, _mockFunctionContext.Object);

        // Assert
        captured.Should().HaveCount(5, "the function creates and sends five messages");

        captured.Should().AllSatisfy(msg =>
        {
            msg.JobId.Should().NotBeEmpty();
            msg.Source.Should().Be("FinanceJobFunction");
            msg.QueuedAt.Should().BeCloseTo(DateTime.UtcNow, precision: TimeSpan.FromSeconds(5));
        });

        _mockFunctionEndpoint.Verify();
    }

    [Test]
    public async Task Run_When_Send_Throws_Logs_Error_And_Rethrows()
    {
        // Arrange
        var expectedException = new InvalidOperationException("boom");

        _mockFunctionEndpoint
            .Setup(x => x.Send(
                It.IsAny<FinanceJobsQueueItem>(), 
                It.IsAny<SendOptions>(), 
                It.IsAny<FunctionContext>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException)
            .Verifiable();

        _mockLogger
            .Setup(x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                expectedException,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()))
            .Verifiable(Times.Once);

        // Act
        Func<Task> act = () => _function.Run(null!, _mockFunctionContext.Object);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");

        // Verify 
        _mockLogger.Verify();
    }
    }
}
