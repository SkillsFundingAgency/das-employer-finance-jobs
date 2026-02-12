using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Helpers;
internal class FakeOrchestrationMetadataAsyncPageable : AsyncPageable<OrchestrationMetadata>
{
    public override async IAsyncEnumerable<Page<OrchestrationMetadata>> AsPages([EnumeratorCancellation] string? continuationToken = null, int? pageSizeHint = null)
    {
        yield break;
    }
}
