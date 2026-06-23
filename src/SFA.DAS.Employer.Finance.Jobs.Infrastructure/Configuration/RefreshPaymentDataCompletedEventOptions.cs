namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Configuration;

public class RefreshPaymentDataCompletedEventOptions
{
    public const string DefaultTopicName = "SFA.DAS.EmployerFinance.Messages.Events.RefreshPaymentDataCompletedEvent";

    public string TopicName { get; set; } = DefaultTopicName;
    public string ConnectionString { get; set; } = string.Empty;
    public string FullyQualifiedNamespace { get; set; } = string.Empty;

    public string GetTopicName() => string.IsNullOrWhiteSpace(TopicName) ? DefaultTopicName : TopicName;
}
