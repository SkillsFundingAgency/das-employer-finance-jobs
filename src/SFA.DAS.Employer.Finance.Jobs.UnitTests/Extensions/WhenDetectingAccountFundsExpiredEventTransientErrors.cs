using Azure.Messaging.ServiceBus;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Extensions;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Extensions;

[TestFixture]
public class WhenDetectingAccountFundsExpiredEventTransientErrors
{
    [Test]
    public void Then_A_Transient_Service_Bus_Failure_Is_Identified()
    {
        var exception = new ServiceBusException(
            "Service Bus timed out",
            ServiceBusFailureReason.ServiceTimeout);

        AccountFundsExpiredEventTransientErrorDetector.IsTransient(exception).Should().BeTrue();
    }

    [Test]
    public void Then_A_NonTransient_Service_Bus_Failure_Is_Not_Identified_As_Transient()
    {
        var exception = new ServiceBusException(
            "Service Bus entity was not found",
            ServiceBusFailureReason.MessagingEntityNotFound);

        AccountFundsExpiredEventTransientErrorDetector.IsTransient(exception).Should().BeFalse();
    }

    [Test]
    public void Then_A_Wrapped_Transient_Failure_Is_Identified()
    {
        var exception = new InvalidOperationException(
            "NServiceBus dispatch failed",
            new TimeoutException("Service Bus timed out"));

        AccountFundsExpiredEventTransientErrorDetector.IsTransient(exception).Should().BeTrue();
    }

    [Test]
    public void Then_An_Ordinary_Application_Failure_Is_Not_Identified_As_Transient()
    {
        var exception = new InvalidOperationException("Invalid Service Bus configuration");

        AccountFundsExpiredEventTransientErrorDetector.IsTransient(exception).Should().BeFalse();
    }
}
