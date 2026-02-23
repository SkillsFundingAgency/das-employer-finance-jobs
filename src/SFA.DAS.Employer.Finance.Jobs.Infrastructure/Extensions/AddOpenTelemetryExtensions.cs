using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.Extensions.DependencyInjection;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Extensions;

public static class AddOpenTelemetryExtensions
{
    public static void AddOpenTelemetryRegistration(this IServiceCollection services, string appInsightsConnectionString)
    {
        if (!string.IsNullOrEmpty(appInsightsConnectionString))
        {
            services.AddOpenTelemetry().UseAzureMonitor(options =>
            {
                options.ConnectionString = appInsightsConnectionString;
            });
        }
    }
}