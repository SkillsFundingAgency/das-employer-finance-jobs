using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Models;

public class ActivityStageResult<T>
{
    public bool Success { get; init; }
    public T? Value { get; init; }
    public ImportLevyFailedItem? FailedItem { get; init; }

    public static ActivityStageResult<T> Succeeded(T value) => new() { Success = true, Value = value };
    public static ActivityStageResult<T> Failed(ImportLevyFailedItem failedItem) => new() { Success = false, FailedItem = failedItem };
}
