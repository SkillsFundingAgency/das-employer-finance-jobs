using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;
using SFA.DAS.Employer.Finance.Jobs.Functions.ProcessAccountPayments.Handlers;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Extensions;

[assembly: NServiceBusTriggerFunction("SFA.DAS.Employer.Finance.Jobs.PAP")]

Console.WriteLine("=== ProcessAccountPayments Function App Starting ===");
Console.WriteLine($"Environment: {Environment.GetEnvironmentVariable("EnvironmentName")}");
Console.WriteLine($"AzureWebJobsStorage: {Environment.GetEnvironmentVariable("AzureWebJobsStorage")}");
Console.WriteLine($"AzureWebJobsServiceBus: {Environment.GetEnvironmentVariable("AzureWebJobsServiceBus")}");
Console.WriteLine($"APPLICATIONINSIGHTS_CONNECTION_STRING: {Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")?.Substring(0, Math.Min(50, Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")?.Length ?? 0))}...");

try
{
var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
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
        
        // Setup Application Insights (following das-recruit-jobs pattern)
        Console.WriteLine("=== Configuring Application Insights ===");
        services.AddApplicationInsightsTelemetryWorkerService(options =>
        {
#if DEBUG
            options.DeveloperMode = true;
#endif
        });
        services.ConfigureFunctionsApplicationInsights();
        
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
    
var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("ProcessAccountPayments Function App starting up with Application Insights (das-recruit-jobs pattern)...");
logger.LogInformation("Endpoint Name: SFA.DAS.Employer.Finance.Jobs.PAP");
logger.LogInformation("Expected Queue: SFA.DAS.Employer.Finance.Jobs.PAP");

// Test Application Insights logging
logger.LogWarning("=== TEST APPLICATION INSIGHTS LOG MESSAGE (das-recruit-jobs pattern) ===");
    
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

