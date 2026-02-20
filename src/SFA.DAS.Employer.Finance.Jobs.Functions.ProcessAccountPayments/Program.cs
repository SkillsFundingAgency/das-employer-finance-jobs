using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Functions.ProcessAccountPayments.Handlers;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Extensions;
using System.Collections.Generic;

[assembly: NServiceBusTriggerFunction("SFA.DAS.Employer.Finance.Jobs.PAP")]

Console.WriteLine("=== ProcessAccountPayments Function App Starting ===");
Console.WriteLine($"Environment: {Environment.GetEnvironmentVariable("EnvironmentName")}");
Console.WriteLine($"AzureWebJobsStorage: {Environment.GetEnvironmentVariable("AzureWebJobsStorage")}");
Console.WriteLine($"AzureWebJobsServiceBus: {Environment.GetEnvironmentVariable("AzureWebJobsServiceBus")}");
Console.WriteLine($"APPLICATIONINSIGHTS_CONNECTION_STRING: {Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")?.Substring(0, Math.Min(50, Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")?.Length ?? 0))}...");

try
{
    var host = new HostBuilder()
        .ConfigureFunctionsWorkerDefaults()
        .ConfigureAppConfiguration(builder => 
        {
            Console.WriteLine("=== Configuring App Configuration ===");
            builder.BuildDasConfiguration();
        })
        .ConfigureNServiceBus("SFA.DAS.Employer.Finance.Jobs.PAP")
        .ConfigureServices((context, services) =>
        {
            Console.WriteLine("=== Configuring Services ===");
            var configuration = context.Configuration;
            
            // DurableTask services removed temporarily to focus on NServiceBus
            // services.AddDurableTaskClient(builder => builder.UseGrpc());
            // services.AddSingleton<IProcessAccountOrchestrationStarter, ProcessAccountOrchestrationStarter>();
            
            // Explicitly register the message handler
            Console.WriteLine("=== Registering Message Handlers ===");
            services.AddTransient<ImportAccountPaymentsCommandHandler>();
            Console.WriteLine("=== ImportAccountPaymentsCommandHandler registered ===");
            
            services.AddDasLogging();
            services.AddDasDataProtection(configuration);
            services.AddConfigurationOptions(configuration);
            services.AddServiceRegistration(configuration);
            Console.WriteLine("=== Services Configured ===");
        })    
        .Build();

    Console.WriteLine("=== ProcessAccountPayments Function App Built Successfully ===");
    
    // Check if Application Insights services are registered
    var aiTelemetryClient = host.Services.GetService<Microsoft.ApplicationInsights.TelemetryClient>();
    Console.WriteLine($"=== Application Insights TelemetryClient registered: {aiTelemetryClient != null} ===");
    if (aiTelemetryClient != null)
    {
        Console.WriteLine($"=== Application Insights InstrumentationKey: {aiTelemetryClient.InstrumentationKey} ===");
        Console.WriteLine($"=== Application Insights ConnectionString: {aiTelemetryClient.TelemetryConfiguration?.ConnectionString?.Substring(0, Math.Min(50, aiTelemetryClient.TelemetryConfiguration?.ConnectionString?.Length ?? 0))}... ===");
    }
    
    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("ProcessAccountPayments Function App starting up...");
    logger.LogInformation("Endpoint Name: SFA.DAS.Employer.Finance.Jobs.PAP");
    logger.LogInformation("Expected Queue: SFA.DAS.Employer.Finance.Jobs.PAP");
    
    // Test Application Insights logging
    logger.LogWarning("=== TEST APPLICATION INSIGHTS LOG MESSAGE ===");
    if (aiTelemetryClient != null)
    {
        aiTelemetryClient.TrackEvent("ProcessAccountPayments-Startup", new Dictionary<string, string>
        {
            ["EndpointName"] = "SFA.DAS.Employer.Finance.Jobs.PAP",
            ["Environment"] = Environment.GetEnvironmentVariable("EnvironmentName") ?? "Unknown"
        });
        aiTelemetryClient.Flush();
        Console.WriteLine("=== Application Insights test event sent ===");
    }
    
    Console.WriteLine("=== Starting Host ===");
    await host.RunAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"=== FATAL ERROR DURING STARTUP ===");
    Console.WriteLine($"Exception Type: {ex.GetType().Name}");
    Console.WriteLine($"Exception Message: {ex.Message}");
    Console.WriteLine($"Stack Trace: {ex.StackTrace}");
    if (ex.InnerException != null)
    {
        Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
    }
    throw;
}

