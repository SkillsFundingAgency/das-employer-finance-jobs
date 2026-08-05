namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Services;

public interface IRetryService
{
    Task<T> ExecuteAsync<T>(
        Func<Task<T>> action,
        string correlationId,
        string operationName,
        int retries = 3);

    Task<T> ExecuteAsync<T>(
        Func<Task<T>> action,
        string correlationId,
        string operationName,
        Func<Exception, bool> shouldRetry,
        int retries = 3);
}
