using Microsoft.DurableTask.Client;
using System;
using System.Reflection;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Helpers;

public static class OrchestrationMetadataHelper
{
    public static OrchestrationMetadata Create(string instanceId, OrchestrationRuntimeStatus runtimeStatus)
    {
        var executionId = Guid.NewGuid().ToString();
        var metadata = new OrchestrationMetadata(executionId, instanceId);
        
        // Use reflection to set the RuntimeStatus property since it's not publicly settable
        var runtimeStatusProperty = typeof(OrchestrationMetadata).GetProperty("RuntimeStatus", BindingFlags.Public | BindingFlags.Instance);
        if (runtimeStatusProperty != null && runtimeStatusProperty.CanWrite)
        {
            runtimeStatusProperty.SetValue(metadata, runtimeStatus);
        }
        else
        {
            // If property is not settable, try using reflection to set a private field
            var field = typeof(OrchestrationMetadata).GetField("_runtimeStatus", BindingFlags.NonPublic | BindingFlags.Instance);
            field?.SetValue(metadata, runtimeStatus);
        }
        
        return metadata;
    }
}

