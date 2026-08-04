using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.UnitTests.Helpers;

internal class FakeOrchestrationMetadataAsyncPageable : AsyncPageable<OrchestrationMetadata>
{
    public override IAsyncEnumerable<Page<OrchestrationMetadata>> AsPages(
        string? continuationToken = null,
        int? pageSizeHint = null)
    {
        return GetEmptyPages();
    }

    private static async IAsyncEnumerable<Page<OrchestrationMetadata>> GetEmptyPages()
    {
        await Task.CompletedTask;
        yield break;
    }
}
