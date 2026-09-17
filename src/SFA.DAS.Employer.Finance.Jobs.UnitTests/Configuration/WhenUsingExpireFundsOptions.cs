using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Configuration;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Configuration;

public class WhenUsingExpireFundsOptions
{
    [TestCase(0, ExpireFundsOptions.DefaultAccountPageSize)]
    [TestCase(-1, ExpireFundsOptions.DefaultAccountPageSize)]
    [TestCase(500, 500)]
    [TestCase(25000, 10000)]
    public void Then_Account_Page_Size_Is_Bounded(int configuredValue, int expectedValue)
    {
        var options = new ExpireFundsOptions { AccountPageSize = configuredValue };

        options.GetAccountPageSize().Should().Be(expectedValue);
    }

    [TestCase(0, ExpireFundsOptions.DefaultMaxConcurrentAccounts)]
    [TestCase(-1, ExpireFundsOptions.DefaultMaxConcurrentAccounts)]
    [TestCase(25, 25)]
    [TestCase(250, 100)]
    public void Then_Max_Concurrent_Accounts_Is_Bounded(int configuredValue, int expectedValue)
    {
        var options = new ExpireFundsOptions { MaxConcurrentAccounts = configuredValue };

        options.GetMaxConcurrentAccounts().Should().Be(expectedValue);
    }
}
