using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using NServiceBus;
using NServiceBus.Configuration.AdvancedExtensibility;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Extensions;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Extensions;

public class WhenUsingNServiceBusExtensions
{
    [Test]
    public void Then_Payments_And_Levy_Endpoint_Names_Are_Distinct()
    {
        NServiceBusExtensions.PaymentsEndpointName.Should().Be("SFA.DAS.EmployerFinance.Jobs.Payments");
        NServiceBusExtensions.LevyEndpointName.Should().Be("SFA.DAS.EmployerFinance.Jobs.Levy");
        NServiceBusExtensions.PaymentsEndpointName.Should().NotBe(NServiceBusExtensions.LevyEndpointName);
        NServiceBusExtensions.PaymentsEndpointName.Should().NotBe("SFA.DAS.EmployerFinance.Jobs.Functions");
    }

    [Test]
    public void Then_Fully_Qualified_Namespace_Is_Read_From_Functions_Service_Bus_Setting()
    {
        var configuration = BuildConfiguration(new KeyValuePair<string, string>("AzureWebJobsServiceBus__fullyQualifiedNamespace", "test.servicebus.windows.net"));

        var result = NServiceBusExtensions.GetFullyQualifiedNamespace(configuration);

        result.Should().Be("test.servicebus.windows.net");
    }

    [Test]
    public void Then_Fully_Qualified_Namespace_Is_Read_From_Nested_Service_Bus_Setting()
    {
        var configuration = BuildConfiguration(new KeyValuePair<string, string>("AzureWebJobsServiceBus:fullyQualifiedNamespace", "nested.servicebus.windows.net"));

        var result = NServiceBusExtensions.GetFullyQualifiedNamespace(configuration);

        result.Should().Be("nested.servicebus.windows.net");
    }

    [Test]
    public void Then_Fully_Qualified_Namespace_Is_Read_From_Plain_Service_Bus_Setting()
    {
        var configuration = BuildConfiguration(new KeyValuePair<string, string>("AzureWebJobsServiceBus", "plain.servicebus.windows.net"));

        var result = NServiceBusExtensions.GetFullyQualifiedNamespace(configuration);

        result.Should().Be("plain.servicebus.windows.net");
    }

    [Test]
    public void Then_Fully_Qualified_Namespace_Is_Extracted_From_Connection_String()
    {
        const string connectionString = "Endpoint=sb://connection.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=abc";

        var result = connectionString.GetFullyQualifiedNamespace();

        result.Should().Be("connection.servicebus.windows.net");
    }

    [Test]
    public void Then_Invalid_Connection_String_Throws()
    {
        const string connectionString = "SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=abc";

        Assert.Throws<FormatException>(() => connectionString.GetFullyQualifiedNamespace());
    }

    [Test]
    public void Then_License_Is_Read_From_NServiceBus_License_Setting()
    {
        var configuration = BuildConfiguration(new KeyValuePair<string, string>("NServiceBusLicense", "license"));

        var result = NServiceBusExtensions.GetLicense(configuration);

        result.Should().Be("license");
    }

    [Test]
    public void Then_License_Is_Read_From_Legacy_Finance_Jobs_Setting()
    {
        var configuration = BuildConfiguration(new KeyValuePair<string, string>("EmployerFinanceJobsConfiguration:NServiceBusLicense", "legacy-license"));

        var result = NServiceBusExtensions.GetLicense(configuration);

        result.Should().Be("legacy-license");
    }

    [Test]
    public void Then_Missing_License_Returns_Empty_String()
    {
        var result = NServiceBusExtensions.GetLicense(BuildConfiguration());

        result.Should().BeEmpty();
    }

    [Test]
    public async Task Then_Host_Startup_Diagnostics_Use_A_No_Op_Writer_Instead_Of_The_File_System()
    {
        var endpointConfiguration = new EndpointConfiguration("SFA.DAS.EmployerFinance.Jobs.Payments");

        endpointConfiguration.ConfigureHostStartupDiagnosticsForAzureFunctions();

        var writer = endpointConfiguration.GetSettings()
            .GetOrDefault<Func<string, CancellationToken, Task>>("HostDiagnosticsWriter");

        writer.Should().NotBeNull();
        await writer("diagnostics", CancellationToken.None);
    }

    [Test]
    public void Then_Host_Startup_Diagnostics_Are_Written_To_The_Log()
    {
        var endpointConfiguration = new EndpointConfiguration("SFA.DAS.EmployerFinance.Jobs.Levy");

        endpointConfiguration.ConfigureHostStartupDiagnosticsForAzureFunctions();

        GetWriteDiagnosticsToLog(endpointConfiguration).Should().BeTrue();
    }

    private static IConfigurationRoot BuildConfiguration(params KeyValuePair<string, string>[] values)
    {
        var configSource = new MemoryConfigurationSource
        {
            InitialData = values
        };
        var provider = new MemoryConfigurationProvider(configSource);

        return new ConfigurationRoot(new List<IConfigurationProvider> { provider });
    }

    private static bool GetWriteDiagnosticsToLog(EndpointConfiguration endpointConfiguration)
    {
        var settings = endpointConfiguration.GetSettings();
        var hostingSettingsType = typeof(EndpointConfiguration).Assembly.GetType("NServiceBus.HostingComponent+Settings");
        var getMethod = settings.GetType()
            .GetMethods()
            .Single(method => method.Name == "Get" && method.IsGenericMethodDefinition && method.GetParameters().Length == 0);
        var hostingSettings = getMethod.MakeGenericMethod(hostingSettingsType).Invoke(settings, null);

        return (bool)hostingSettingsType.GetProperty("WriteDiagnosticsToLog").GetValue(hostingSettings);
    }
}
