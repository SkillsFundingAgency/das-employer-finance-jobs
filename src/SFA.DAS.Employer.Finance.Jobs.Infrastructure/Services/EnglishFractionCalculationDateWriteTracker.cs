using System.Collections.Concurrent;
using System.Globalization;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;

public class EnglishFractionCalculationDateWriteTracker : IEnglishFractionCalculationDateWriteTracker
{
    private readonly ConcurrentDictionary<string, WriteState> _writes = new();
    private readonly object _lock = new();

    public bool TryStartWrite(string correlationId, DateTime calculationDate)
    {
        var key = BuildKey(correlationId, calculationDate);

        lock (_lock)
        {
            return _writes.TryAdd(key, WriteState.InProgress);
        }
    }

    public void MarkWriteSucceeded(string correlationId, DateTime calculationDate)
    {
        var key = BuildKey(correlationId, calculationDate);
        _writes[key] = WriteState.Completed;
    }

    public void MarkWriteFailed(string correlationId, DateTime calculationDate)
    {
        var key = BuildKey(correlationId, calculationDate);
        _writes.TryRemove(key, out _);
    }

    private static string BuildKey(string correlationId, DateTime calculationDate)
    {
        return $"{correlationId}|{calculationDate.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";
    }

    private enum WriteState
    {
        InProgress = 1,
        Completed = 2
    }
}
