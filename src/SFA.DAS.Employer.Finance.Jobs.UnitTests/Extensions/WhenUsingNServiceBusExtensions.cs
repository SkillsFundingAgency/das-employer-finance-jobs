using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Extensions;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Extensions;

public class WhenUsingNServiceBusExtensions
{
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
