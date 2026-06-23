using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Configuration;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Configuration;

public class WhenUsingRefreshPaymentDataCompletedEventOptions
{
    [Test]
    public void Then_Default_Topic_Name_Uses_Refresh_Payment_Data_Completed_Event_Contract()
    {
        var options = new RefreshPaymentDataCompletedEventOptions();

        options.GetTopicName().Should().Be(RefreshPaymentDataCompletedEventOptions.DefaultTopicName);
    }

    [Test]
    public void Then_Configured_Topic_Name_Is_Used()
    {
        var options = new RefreshPaymentDataCompletedEventOptions
        {
            TopicName = "custom-topic"
        };

        options.GetTopicName().Should().Be("custom-topic");
    }
}
