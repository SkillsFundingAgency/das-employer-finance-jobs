using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Activities;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.UnitTests.Helpers;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.UnitTests.Activities;

[TestFixture]
public class ProcessLevyPayeSchemeActivityTests
{
    [Test]
    public async Task Run_Logs_The_Fanned_Out_Work_Item()
    {
        var logger = new Mock<ILogger<ProcessLevyPayeSchemeActivity>>();
        var activity = new ProcessLevyPayeSchemeActivity(logger.Object);

        await activity.Run(new ProcessLevyPayeSchemeInput
        {
            CorrelationId = "corr-123",
            AccountId = 77,
            PayeSchemeReference = "123/AB456"
        });

        logger.VerifyLogContains("Queued downstream PAYE work item");
    }
}
