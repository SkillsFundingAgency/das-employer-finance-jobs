using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Extensions;

public static class LoggingExtensions
{
    public static IServiceCollection AddDasLogging(this IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.AddFilter("Microsoft", LogLevel.Warning);
            builder.AddFilter("System", LogLevel.Warning);
            builder.AddFilter("NServiceBus", LogLevel.Debug);
            builder.AddFilter("StartupDiagnostics", LogLevel.Information);
            builder.AddFilter("SFA.DAS", LogLevel.Information);

            builder.SetMinimumLevel(LogLevel.Trace);
            builder.AddConsole();
        });

        return services;
    }
}