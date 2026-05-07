namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Services;

public interface IRetryDelay
{
    Task DelayAsync(TimeSpan delay);
}
