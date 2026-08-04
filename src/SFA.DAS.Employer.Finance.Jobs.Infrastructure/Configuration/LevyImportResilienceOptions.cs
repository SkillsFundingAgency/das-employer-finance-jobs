namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Configuration;

public class LevyImportResilienceOptions
{
    public const string SectionName = "LevyImportResilience";
    public int MaxRetries { get; set; } = 4;
    public int BaseDelayMilliseconds { get; set; } = 500;
    public int JitterMilliseconds { get; set; } = 250;
    public int MaxRequestsPerWindow { get; set; } = 6;
    public int WindowSeconds { get; set; } = 2;
}
