using System.Threading;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests.FundingProjection;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses.FundingProjection;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Services;

[TestFixture]
public class WhenCallingEmployerFinanceJobsOuterService
{
    [Test]
    public async Task ImportCommittedLearnersAsync_WhenCalled_ShouldPostAccountRequest()
    {
        // Arrange
        var apiClient = new Mock<IEmployerFinanceJobsOuterApiClient>();
        var service = new EmployerFinanceJobsOuterService(apiClient.Object, Mock.Of<ILogger<EmployerFinanceJobsOuterService>>());
        var response = new ImportCommittedLearnersResponse { IsSuccessful = true };
        apiClient.Setup(client => client.Post<ImportCommittedLearnersResponse>(
                It.Is<ImportCommittedLearnersRequest>(request => request.Payload.AccountId == 12345), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await service.ImportCommittedLearnersAsync(12345, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(response);
        apiClient.Verify(client => client.Post<ImportCommittedLearnersResponse>(
            It.Is<ImportCommittedLearnersRequest>(request => request.Payload.AccountId == 12345),
            CancellationToken.None), Times.Once);
    }

    [Test]
    public async Task PostJobAsync_WhenCalled_ShouldPostJobRequest()
    {
        // Arrange
        var apiClient = new Mock<IEmployerFinanceJobsOuterApiClient>();
        var service = new EmployerFinanceJobsOuterService(apiClient.Object, Mock.Of<ILogger<EmployerFinanceJobsOuterService>>());
        var jobId = Guid.NewGuid();
        var payload = new PostJobRequestData
        {
            AccountId = 12345,
            JobName = "ImportLearnersCost",
            JobState = "Completed",
            LastSuccessfulImportDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            LastAttemptedDate = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            LastAttemptSuccessful = true,
            TotalRecordsLastRun = 10,
            FailedRecordsLastRun = 0
        };

        // Act
        await service.PostJobAsync(jobId, payload, CancellationToken.None);

        // Assert
        apiClient.Verify(client => client.Post(
            It.Is<PostJobRequest>(request => request.JobId == jobId && request.Payload == payload),
            CancellationToken.None), Times.Once);
    }
}
