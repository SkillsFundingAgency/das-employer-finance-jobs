namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Configuration;

public class ImportLevyProcessingOptions
{
    public const string SectionName = "ImportLevyProcessing";
    public int MaxConcurrentHmrcActivities { get; set; } = 25;
}