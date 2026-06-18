using Microsoft.DurableTask.Client;
using System.Reflection;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.UnitTests.Helpers;

public static class OrchestrationMetadataHelper
{
    public static OrchestrationMetadata Create(string instanceId, OrchestrationRuntimeStatus runtimeStatus)
    {
        var metadata = new OrchestrationMetadata(Guid.NewGuid().ToString(), instanceId);

        var runtimeStatusProperty = typeof(OrchestrationMetadata).GetProperty("RuntimeStatus", BindingFlags.Public | BindingFlags.Instance);
        if (runtimeStatusProperty != null && runtimeStatusProperty.CanWrite)
        {
            runtimeStatusProperty.SetValue(metadata, runtimeStatus);
        }
        else
        {
            var field = typeof(OrchestrationMetadata).GetField("_runtimeStatus", BindingFlags.NonPublic | BindingFlags.Instance);
            field?.SetValue(metadata, runtimeStatus);
        }

        return metadata;
    }
}
