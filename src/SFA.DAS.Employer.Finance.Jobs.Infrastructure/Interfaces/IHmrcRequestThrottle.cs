namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

public interface IHmrcRequestThrottle
{
    Task WaitAsync(string operationName, CancellationToken cancellationToken = default);
}
