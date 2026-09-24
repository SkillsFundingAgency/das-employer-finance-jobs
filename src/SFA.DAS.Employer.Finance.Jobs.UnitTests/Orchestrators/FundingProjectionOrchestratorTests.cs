using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Functions.Activities;
using SFA.DAS.Employer.Finance.Jobs.Functions.Orchestrators;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses.FundingProjection;
using System.Collections.Generic;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Orchestrators;

[TestFixture]
public class FundingProjectionOrchestratorWhenRunning
{
    [Test]
    public async Task Run_ShouldCallActivitiesInOrder()
    {
        // Arrange
        var context = new Mock<TaskOrchestrationContext>();
        var logger = new Mock<ILogger<FundingProjectionOrchestrator>>();
        var activityCalls = new List<string>();

        context
            .Setup(x => x.CallActivityAsync<ImportCommittedLearnersResponse>(
                It.IsAny<TaskName>(), It.IsAny<object>(), It.IsAny<TaskOptions>()))
            .Callback<TaskName, object, TaskOptions>((name, _, _) => activityCalls.Add(name.Name))
            .ReturnsAsync(new ImportCommittedLearnersResponse { TotalRecords = 1 });
        context
            .Setup(x => x.CallActivityAsync<UpdateCommittedLearnersCostResponse>(
                It.IsAny<TaskName>(), It.IsAny<object>(), It.IsAny<TaskOptions>()))
            .Callback<TaskName, object, TaskOptions>((name, _, _) => activityCalls.Add(name.Name))
            .ReturnsAsync(new UpdateCommittedLearnersCostResponse());
        context
            .Setup(x => x.CallActivityAsync<RecalculateFundingProjectionResponse>(
                It.IsAny<TaskName>(), It.IsAny<object>(), It.IsAny<TaskOptions>()))
            .Callback<TaskName, object, TaskOptions>((name, _, _) => activityCalls.Add(name.Name))
            .ReturnsAsync(new RecalculateFundingProjectionResponse());

        var orchestrator = new FundingProjectionOrchestrator(logger.Object);

        // Act
        await orchestrator.Run(context.Object);

        // Assert
        activityCalls.Should().Equal(
            nameof(FundingProjectionActivities.ImportCommittedLearnersActivity),
            nameof(FundingProjectionActivities.UpdateCommittedLearnersCostActivity),
            nameof(FundingProjectionActivities.RecalculateFundingProjectionActivity));
    }

    [Test]
    public async Task Run_WhenImportHasNoSuccessfulRecords_ShouldNotCallLaterActivities()
    {
        // Arrange
        var context = new Mock<TaskOrchestrationContext>();
        var logger = new Mock<ILogger<FundingProjectionOrchestrator>>();
        context
            .Setup(x => x.CallActivityAsync<ImportCommittedLearnersResponse>(
                It.IsAny<TaskName>(), It.IsAny<object>(), It.IsAny<TaskOptions>()))
            .ReturnsAsync(new ImportCommittedLearnersResponse { TotalRecords = 10, FailedRecords = 10 });

        var orchestrator = new FundingProjectionOrchestrator(logger.Object);

        // Act
        await orchestrator.Run(context.Object);

        // Assert
        context.Verify(x => x.CallActivityAsync<UpdateCommittedLearnersCostResponse>(
            It.IsAny<TaskName>(), It.IsAny<object>(), It.IsAny<TaskOptions>()), Times.Never);
        context.Verify(x => x.CallActivityAsync<RecalculateFundingProjectionResponse>(
            It.IsAny<TaskName>(), It.IsAny<object>(), It.IsAny<TaskOptions>()), Times.Never);
    }

    [Test]
    public async Task Run_WhenImportCommittedLearnersFails_ShouldNotCallLaterActivities()
    {
        // Arrange
        var context = new Mock<TaskOrchestrationContext>();
        var logger = new Mock<ILogger<FundingProjectionOrchestrator>>();
        context
            .Setup(x => x.CallActivityAsync<ImportCommittedLearnersResponse>(
                It.IsAny<TaskName>(), It.IsAny<object>(), It.IsAny<TaskOptions>()))
            .ThrowsAsync(new InvalidOperationException("Import failed"));

        var orchestrator = new FundingProjectionOrchestrator(logger.Object);

        // Act
        var act = () => orchestrator.Run(context.Object);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Import failed");
        context.Verify(x => x.CallActivityAsync<UpdateCommittedLearnersCostResponse>(
            It.IsAny<TaskName>(), It.IsAny<object>(), It.IsAny<TaskOptions>()), Times.Never);
        context.Verify(x => x.CallActivityAsync<RecalculateFundingProjectionResponse>(
            It.IsAny<TaskName>(), It.IsAny<object>(), It.IsAny<TaskOptions>()), Times.Never);
    }
}