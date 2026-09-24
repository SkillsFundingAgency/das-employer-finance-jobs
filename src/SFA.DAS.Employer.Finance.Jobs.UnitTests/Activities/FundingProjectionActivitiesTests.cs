using System.Threading;
using AutoFixture.NUnit4;
using SFA.DAS.Employer.Finance.Jobs.Functions.Activities;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses.FundingProjection;
using SFA.DAS.Testing.AutoFixture;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Activities;

[TestFixture]
public class FundingProjectionActivitiesWhenCallingEmployerFinanceJobsService
{
    [Test, MoqAutoData]
    public async Task ImportCommittedLearnersActivity_ShouldCallImportCommittedLearnersAsync(
        ImportCommittedLearnersResponse response,
        [Frozen] Mock<IEmployerFinanceJobsOuterService> service,
        [Greedy] FundingProjectionActivities activities,
        CancellationToken cancellationToken)
    {
        // Arrange
        service
            .Setup(x => x.ImportCommittedLearnersAsync(cancellationToken))
            .ReturnsAsync(response);

        // Act
        var result = await activities.ImportCommittedLearnersActivity(new object(), cancellationToken);

        // Assert
        result.Should().BeEquivalentTo(response);
        service.Verify(x => x.ImportCommittedLearnersAsync(cancellationToken), Times.Once);
    }

    [Test, MoqAutoData]
    public async Task UpdateCommittedLearnersCostActivity_ShouldCallUpdateCommittedLearnersCostAsync(
        UpdateCommittedLearnersCostResponse response,
        [Frozen] Mock<IEmployerFinanceJobsOuterService> service,
        [Greedy] FundingProjectionActivities activities,
        CancellationToken cancellationToken)
    {
        // Arrange
        service
            .Setup(x => x.UpdateCommittedLearnersCostAsync(cancellationToken))
            .ReturnsAsync(response);

        // Act
        var result = await activities.UpdateCommittedLearnersCostActivity(new object(), cancellationToken);

        // Assert
        result.Should().BeEquivalentTo(response);
        service.Verify(x => x.UpdateCommittedLearnersCostAsync(cancellationToken), Times.Once);
    }

    [Test, MoqAutoData]
    public async Task RecalculateFundingProjectionActivity_ShouldCallRecalculateFundingProjectionAsync(RecalculateFundingProjectionResponse response,
        [Frozen] Mock<IEmployerFinanceJobsOuterService> service,
        [Greedy] FundingProjectionActivities activities,
        CancellationToken cancellationToken)
    {
        // Arrange
        service 
            .Setup(x => x.RecalculateFundingProjectionAsync(cancellationToken))
            .ReturnsAsync(response);

        // Act
        var result = await activities.RecalculateFundingProjectionActivity(new object(), cancellationToken);

        // Assert
        result.Should().Be(response);
        service.Verify(x => x.RecalculateFundingProjectionAsync(cancellationToken), Times.Once);
    }
}
