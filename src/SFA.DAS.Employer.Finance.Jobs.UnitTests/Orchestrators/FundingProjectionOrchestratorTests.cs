using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Functions.Activities;
using SFA.DAS.Employer.Finance.Jobs.Functions.Models;
using SFA.DAS.Employer.Finance.Jobs.Functions.Orchestrators;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses.FundingProjection;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Orchestrators;

[TestFixture]
public class FundingProjectionOrchestratorWhenRunning
{
    [Test]
    public async Task Run_WhenImportSucceeds_ShouldPostCompletedJobState()
    {
        // Arrange
        var context = new Mock<TaskOrchestrationContext>();
        var input = new ImportLearnersCostEvent(Guid.NewGuid(), 12345);
        var response = new ImportCommittedLearnersResponse
        {
            IsSuccessful = true,
            TotalRecordsProcessed = 10,
            FailedRecordsCount = 0,
            AttemptedDate = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            LastSuccessfulImportDate = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc)
        };
        context.Setup(x => x.GetInput<ImportLearnersCostEvent>()).Returns(input);
        context.Setup(x => x.CallActivityAsync<ImportCommittedLearnersResponse>(
                nameof(FundingProjectionActivities.ImportCommittedLearnersActivity), input, It.IsAny<TaskOptions>()))
            .ReturnsAsync(response);

        var orchestrator = new FundingProjectionOrchestrator(Mock.Of<ILogger<FundingProjectionOrchestrator>>());

        // Act
        await orchestrator.Run(context.Object);

        // Assert
        context.Verify(x => x.CallActivityAsync(
            nameof(FundingProjectionActivities.PostJobActivity),
            It.Is<PostJobActivityInput>(value =>
                value.JobId == input.JobId &&
                value.Payload.AccountId == input.AccountId &&
                value.Payload.JobState == "Completed" &&
                value.Payload.LastAttemptSuccessful),
            It.IsAny<TaskOptions>()), Times.Once);
    }

    [Test]
    public async Task Run_WhenImportReturnsFailure_ShouldPostFailedJobState()
    {
        // Arrange
        var context = new Mock<TaskOrchestrationContext>();
        var input = new ImportLearnersCostEvent(Guid.NewGuid(), 12345);
        context.Setup(x => x.GetInput<ImportLearnersCostEvent>()).Returns(input);
        context.Setup(x => x.CallActivityAsync<ImportCommittedLearnersResponse>(
                nameof(FundingProjectionActivities.ImportCommittedLearnersActivity), input, It.IsAny<TaskOptions>()))
            .ReturnsAsync(new ImportCommittedLearnersResponse
            {
                IsSuccessful = false,
                FailedRecordsCount = 10,
                AttemptedDate = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            });

        var orchestrator = new FundingProjectionOrchestrator(Mock.Of<ILogger<FundingProjectionOrchestrator>>());

        // Act
        await orchestrator.Run(context.Object);

        // Assert
        context.Verify(x => x.CallActivityAsync(
            nameof(FundingProjectionActivities.PostJobActivity),
            It.Is<PostJobActivityInput>(value =>
                value.Payload.JobState == "Failed" &&
                !value.Payload.LastAttemptSuccessful),
            It.IsAny<TaskOptions>()), Times.Once);
    }
}