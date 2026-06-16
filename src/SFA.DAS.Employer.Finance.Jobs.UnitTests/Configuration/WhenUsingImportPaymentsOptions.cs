using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Configuration;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Configuration;

public class WhenUsingImportPaymentsOptions
{
    [TestCase(0, ImportPaymentsOptions.DefaultMaxConcurrentAccounts)]
    [TestCase(-1, ImportPaymentsOptions.DefaultMaxConcurrentAccounts)]
    [TestCase(25, 25)]
    [TestCase(250, 100)]
    public void Then_Max_Concurrent_Accounts_Is_Bounded(int configuredValue, int expectedValue)
    {
        var options = new ImportPaymentsOptions { MaxConcurrentAccounts = configuredValue };

        options.GetMaxConcurrentAccounts().Should().Be(expectedValue);
    }

    [TestCase(0, ImportPaymentsOptions.DefaultMaxConcurrentPeriodEnds)]
    [TestCase(-1, ImportPaymentsOptions.DefaultMaxConcurrentPeriodEnds)]
    [TestCase(3, 3)]
    [TestCase(30, 10)]
    public void Then_Max_Concurrent_Period_Ends_Is_Bounded(int configuredValue, int expectedValue)
    {
        var options = new ImportPaymentsOptions { MaxConcurrentPeriodEnds = configuredValue };

        options.GetMaxConcurrentPeriodEnds().Should().Be(expectedValue);
    }

    [Test]
    public void Then_Timeouts_Use_Defaults_When_Configured_As_Invalid()
    {
        var options = new ImportPaymentsOptions
        {
            ActiveInstanceInactivityThresholdMinutes = 0,
            StaleInstanceTerminationTimeoutSeconds = 0
        };

        options.GetActiveInstanceInactivityThreshold().Should().Be(TimeSpan.FromMinutes(ImportPaymentsOptions.DefaultActiveInstanceInactivityThresholdMinutes));
        options.GetStaleInstanceTerminationTimeout().Should().Be(TimeSpan.FromSeconds(ImportPaymentsOptions.DefaultStaleInstanceTerminationTimeoutSeconds));
    }
}
