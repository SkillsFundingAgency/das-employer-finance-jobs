namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Configuration;

public class ImportPaymentsOptions
{
    public const int DefaultMaxConcurrentAccounts = 50;
    public const int DefaultMaxConcurrentPeriodEnds = 5;
    public const int DefaultActiveInstanceInactivityThresholdMinutes = 90;
    public const int DefaultStaleInstanceTerminationTimeoutSeconds = 30;

    private const int MaxAllowedConcurrentAccounts = 100;
    private const int MaxAllowedConcurrentPeriodEnds = 10;

    public int MaxConcurrentAccounts { get; set; } = DefaultMaxConcurrentAccounts;
    public int MaxConcurrentPeriodEnds { get; set; } = DefaultMaxConcurrentPeriodEnds;
    public int ActiveInstanceInactivityThresholdMinutes { get; set; } = DefaultActiveInstanceInactivityThresholdMinutes;
    public int StaleInstanceTerminationTimeoutSeconds { get; set; } = DefaultStaleInstanceTerminationTimeoutSeconds;
    public bool AdminEndpointsEnabled { get; set; }
    public bool TransferStagedToOperationalProcessingEnabled { get; set; }

    // TEMP: remove after APPMAN-2773 demo validation — set to null to process all accounts.
    public long? TargetAccountId { get; set; } = 14331;

    public int GetMaxConcurrentAccounts() => GetMaxConcurrentAccountsOrDefault(MaxConcurrentAccounts);

    public int GetMaxConcurrentPeriodEnds() => GetMaxConcurrentPeriodEndsOrDefault(MaxConcurrentPeriodEnds);

    public TimeSpan GetActiveInstanceInactivityThreshold() =>
        TimeSpan.FromMinutes(GetPositiveOrDefault(ActiveInstanceInactivityThresholdMinutes, DefaultActiveInstanceInactivityThresholdMinutes));

    public TimeSpan GetStaleInstanceTerminationTimeout() =>
        TimeSpan.FromSeconds(GetPositiveOrDefault(StaleInstanceTerminationTimeoutSeconds, DefaultStaleInstanceTerminationTimeoutSeconds));

    public static int GetMaxConcurrentAccountsOrDefault(int? value) =>
        GetBoundedOrDefault(value, DefaultMaxConcurrentAccounts, MaxAllowedConcurrentAccounts);

    public static int GetMaxConcurrentPeriodEndsOrDefault(int? value) =>
        GetBoundedOrDefault(value, DefaultMaxConcurrentPeriodEnds, MaxAllowedConcurrentPeriodEnds);

    private static int GetBoundedOrDefault(int? value, int defaultValue, int maxValue)
    {
        if (!value.HasValue || value.Value <= 0)
        {
            return defaultValue;
        }

        return Math.Min(value.Value, maxValue);
    }

    private static int GetPositiveOrDefault(int value, int defaultValue) =>
        value <= 0 ? defaultValue : value;
}
