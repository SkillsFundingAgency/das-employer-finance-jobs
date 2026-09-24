using Azure.Messaging.ServiceBus;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Extensions;

public static class AccountFundsExpiredEventTransientErrorDetector
{
    public static bool IsTransient(Exception exception) =>
        exception switch
        {
            ServiceBusException serviceBusException when serviceBusException.IsTransient => true,
            TimeoutException => true,
            TaskCanceledException => true,
            _ when exception.InnerException is not null => IsTransient(exception.InnerException),
            _ => false
        };
}
