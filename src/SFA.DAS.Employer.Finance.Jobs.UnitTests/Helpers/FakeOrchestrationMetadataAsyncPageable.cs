using System.Collections.Generic;
using System.Linq;
using Dasync.Collections;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Helpers;
internal class FakeOrchestrationMetadataAsyncPageable : AsyncPageable<OrchestrationMetadata>
{
    public override IAsyncEnumerable<Page<OrchestrationMetadata>> AsPages(string? continuationToken = null, int? pageSizeHint = null)
    {
        return AsyncEnumerable.Empty<Page<OrchestrationMetadata>>();
    }
}
