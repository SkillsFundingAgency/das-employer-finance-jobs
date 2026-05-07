namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Services;

public class RetryDelay : IRetryDelay
{
    public Task DelayAsync(TimeSpan delay)
    {
        return Task.Delay(delay);
    }
}
