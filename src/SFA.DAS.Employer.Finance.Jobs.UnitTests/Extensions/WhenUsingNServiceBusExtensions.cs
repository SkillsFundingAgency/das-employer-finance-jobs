using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Extensions;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Extensions;

public class WhenUsingNServiceBusExtensions
{
    [Test]
    public void Then_Function_App_Endpoint_Names_Are_Distinct()
    {
        NServiceBusExtensions.PaymentsEndpointName.Should().Be("SFA.DAS.EmployerFinance.Jobs.Payments");
        NServiceBusExtensions.LevyEndpointName.Should().Be("SFA.DAS.EmployerFinance.Jobs.Levy");
        NServiceBusExtensions.ExpireFundsEndpointName.Should().Be("SFA.DAS.EmployerFinance.Jobs.ExpireFunds");
        NServiceBusExtensions.PaymentsEndpointName.Should().NotBe(NServiceBusExtensions.LevyEndpointName);
        NServiceBusExtensions.PaymentsEndpointName.Should().NotBe(NServiceBusExtensions.ExpireFundsEndpointName);
        NServiceBusExtensions.LevyEndpointName.Should().NotBe(NServiceBusExtensions.ExpireFundsEndpointName);
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

    private static IConfigurationRoot BuildConfiguration(params KeyValuePair<string, string>[] values)
    {
        var configSource = new MemoryConfigurationSource
        {
            InitialData = values
        };
        var provider = new MemoryConfigurationProvider(configSource);

        return new ConfigurationRoot(new List<IConfigurationProvider> { provider });
    }
}
