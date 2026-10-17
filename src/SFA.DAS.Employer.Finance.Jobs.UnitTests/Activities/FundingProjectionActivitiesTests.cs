using System.Threading;
using SFA.DAS.Employer.Finance.Jobs.Functions.FundingProjection.Activities;
using SFA.DAS.Employer.Finance.Jobs.Functions.FundingProjection.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests.FundingProjection;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses.FundingProjection;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Activities;

[TestFixture]
public class FundingProjectionActivitiesWhenCallingEmployerFinanceJobsService
{
    [Test]
    public async Task ImportCommittedLearnersActivity_WhenCalled_ShouldPassAccountIdToService()
    {
        // Arrange
        var service = new Mock<IEmployerFinanceJobsOuterService>();
        var activities = new FundingProjectionActivities(service.Object);
        var input = new ImportLearnersCostEvent(Guid.NewGuid(), 12345);
        var response = new ImportCommittedLearnersResponse { IsSuccessful = true };
        service.Setup(x => x.ImportCommittedLearnersAsync(input.AccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await activities.ImportCommittedLearnersActivity(input, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(response);
        service.Verify(x => x.ImportCommittedLearnersAsync(input.AccountId, CancellationToken.None), Times.Once);
    }

    [Test]
    public async Task PostJobActivity_WhenCalled_ShouldPassJobAndPayloadToService()
    {
        // Arrange
        var service = new Mock<IEmployerFinanceJobsOuterService>();
        var activities = new FundingProjectionActivities(service.Object);
        var input = new PostJobActivityInput(Guid.NewGuid(), new PostJobRequestData
        {
            AccountId = 12345,
            JobName = "ImportLearnersCost",
            JobState = "Completed",
            LastSuccessfulImportDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            LastAttemptedDate = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            LastAttemptSuccessful = true,
            TotalRecordsLastRun = 10,
            FailedRecordsLastRun = 0
        });

        // Act
        await activities.PostJobActivity(input, CancellationToken.None);

        // Assert
        service.Verify(x => x.PostJobAsync(input.JobId, input.Payload, CancellationToken.None), Times.Once);
    }
}
