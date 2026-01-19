using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.ApplicationInsights;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.Extensions;

public static class LoggingExtensions
{
    public static IServiceCollection AddDasLogging(this IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.AddFilter<ApplicationInsightsLoggerProvider>(string.Empty, LogLevel.Information);
            builder.AddFilter<ApplicationInsightsLoggerProvider>("Microsoft", LogLevel.Information);

            builder.AddFilter(typeof(Program).Namespace, LogLevel.Information);
            builder.SetMinimumLevel(LogLevel.Trace);
            builder.AddConsole();
        });

        services
            .AddApplicationInsightsTelemetryWorkerService()
            .ConfigureFunctionsApplicationInsights();

        return services;
    }
}
