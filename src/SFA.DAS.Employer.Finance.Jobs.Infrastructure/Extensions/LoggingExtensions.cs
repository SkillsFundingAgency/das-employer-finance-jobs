using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.ApplicationInsights;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Extensions;

public static class LoggingExtensions
{
    public static IServiceCollection AddDasLogging(this IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            // Application Insights: keep business logs, suppress Azure SDK / durable framework noise.
            builder.AddFilter<ApplicationInsightsLoggerProvider>(string.Empty, LogLevel.Information);
            builder.AddFilter<ApplicationInsightsLoggerProvider>("Microsoft", LogLevel.Warning);
            builder.AddFilter<ApplicationInsightsLoggerProvider>("Azure", LogLevel.Warning);
            builder.AddFilter<ApplicationInsightsLoggerProvider>("Azure.Core", LogLevel.Warning);
            builder.AddFilter<ApplicationInsightsLoggerProvider>("Azure.Storage", LogLevel.Warning);
            builder.AddFilter<ApplicationInsightsLoggerProvider>("Microsoft.Azure.WebJobs", LogLevel.Warning);
            builder.AddFilter<ApplicationInsightsLoggerProvider>("Microsoft.DurableTask", LogLevel.Warning);
            builder.AddFilter<ApplicationInsightsLoggerProvider>("DurableTask", LogLevel.Warning);
            builder.AddFilter<ApplicationInsightsLoggerProvider>("SFA.DAS", LogLevel.Information);

            // General filters
            builder.AddFilter("Microsoft", LogLevel.Warning);
            builder.AddFilter("Azure", LogLevel.Warning);
            builder.AddFilter("Azure.Core", LogLevel.Warning);
            builder.AddFilter("Azure.Storage", LogLevel.Warning);
            builder.AddFilter("Microsoft.Azure.WebJobs", LogLevel.Warning);
            builder.AddFilter("Microsoft.DurableTask", LogLevel.Warning);
            builder.AddFilter("System", LogLevel.Warning);
            builder.AddFilter("StartupDiagnostics", LogLevel.Information);
            builder.AddFilter("SFA.DAS", LogLevel.Information);

            builder.SetMinimumLevel(LogLevel.Trace);
            builder.AddConsole();
        });

        return services;
    }
}
