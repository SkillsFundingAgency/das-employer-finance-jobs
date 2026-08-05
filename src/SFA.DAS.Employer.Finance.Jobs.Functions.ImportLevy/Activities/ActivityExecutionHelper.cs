namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Activities;

internal static class ActivityExecutionHelper
{
    public static Guid ParseCorrelationIdOrNew(string correlationId)
    {
        return Guid.TryParse(correlationId, out var parsedCorrelationId)
            ? parsedCorrelationId
            : Guid.NewGuid();
    }
}
