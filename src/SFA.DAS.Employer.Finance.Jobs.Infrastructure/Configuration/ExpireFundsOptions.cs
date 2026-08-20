namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Configuration;

public class ExpireFundsOptions
{
    public const int DefaultAccountPageSize = 1000;
    public const int DefaultMaxConcurrentAccounts = 50;

    private const int MaxAllowedAccountPageSize = 10000;
    private const int MaxAllowedConcurrentAccounts = 100;

    public int AccountPageSize { get; set; } = DefaultAccountPageSize;
    public int MaxConcurrentAccounts { get; set; } = DefaultMaxConcurrentAccounts;

    public int GetAccountPageSize() => GetAccountPageSizeOrDefault(AccountPageSize);

    public int GetMaxConcurrentAccounts() => GetMaxConcurrentAccountsOrDefault(MaxConcurrentAccounts);

    public static int GetAccountPageSizeOrDefault(int? value) =>
        GetBoundedOrDefault(value, DefaultAccountPageSize, MaxAllowedAccountPageSize);

    public static int GetMaxConcurrentAccountsOrDefault(int? value) =>
        GetBoundedOrDefault(value, DefaultMaxConcurrentAccounts, MaxAllowedConcurrentAccounts);

    private static int GetBoundedOrDefault(int? value, int defaultValue, int maximumValue)
    {
        if (!value.HasValue || value.Value <= 0)
        {
            return defaultValue;
        }

        return Math.Min(value.Value, maximumValue);
    }
}
