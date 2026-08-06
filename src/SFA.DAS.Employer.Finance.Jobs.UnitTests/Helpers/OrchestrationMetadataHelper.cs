using Microsoft.DurableTask.Client;
using System.Reflection;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Helpers;

public static class OrchestrationMetadataHelper
{
    public static OrchestrationMetadata Create(
        string instanceId,
        OrchestrationRuntimeStatus runtimeStatus,
        DateTimeOffset? createdAt = null,
        DateTimeOffset? lastUpdatedAt = null)
    {
        var executionId = Guid.NewGuid().ToString();
        var metadata = new OrchestrationMetadata(executionId, instanceId);

        SetValue(metadata, "RuntimeStatus", runtimeStatus);

        if (createdAt.HasValue)
        {
            SetValue(metadata, "CreatedAt", createdAt.Value);
        }

        if (lastUpdatedAt.HasValue)
        {
            SetValue(metadata, "LastUpdatedAt", lastUpdatedAt.Value);
        }

        return metadata;
    }

    private static void SetValue(OrchestrationMetadata metadata, string memberName, object value)
    {
        var property = typeof(OrchestrationMetadata).GetProperty(memberName, BindingFlags.Public | BindingFlags.Instance);
        if (property != null && property.CanWrite)
        {
            property.SetValue(metadata, value);
            return;
        }

        var backingFieldName = $"<{memberName}>k__BackingField";
        var backingField = typeof(OrchestrationMetadata).GetField(backingFieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (backingField != null)
        {
            backingField.SetValue(metadata, value);
            return;
        }

        var legacyFieldName = $"_{char.ToLowerInvariant(memberName[0])}{memberName[1..]}";
        var legacyField = typeof(OrchestrationMetadata).GetField(legacyFieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        legacyField?.SetValue(metadata, value);
    }
}
