namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;

public abstract class BaseResponse<T> : ErrorResponse
{
    public T Result { get; set; }
}
public abstract class ErrorResponse
{
    public bool IsValid { get; set; }
    public Exception Exception { get; set; }
}
