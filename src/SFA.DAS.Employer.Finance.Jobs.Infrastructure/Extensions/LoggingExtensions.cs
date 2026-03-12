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
            builder.AddApplicationInsights();
            //// Application Insights filters (following das-recruit-jobs pattern)
            //builder.AddFilter<ApplicationInsightsLoggerProvider>(string.Empty, LogLevel.Information);
            //builder.AddFilter<ApplicationInsightsLoggerProvider>("Microsoft", LogLevel.Information);
            //builder.AddFilter<ApplicationInsightsLoggerProvider>("SFA.DAS", LogLevel.Information);
            
            //// General filters
            //builder.AddFilter("Microsoft", LogLevel.Warning);
            //builder.AddFilter("System", LogLevel.Warning);
            //builder.AddFilter("NServiceBus", LogLevel.Debug);
            //builder.AddFilter("StartupDiagnostics", LogLevel.Information);
            //builder.AddFilter("SFA.DAS", LogLevel.Information);

            //builder.SetMinimumLevel(LogLevel.Trace);
            builder.AddConsole();
        });

        return services;
    }
}