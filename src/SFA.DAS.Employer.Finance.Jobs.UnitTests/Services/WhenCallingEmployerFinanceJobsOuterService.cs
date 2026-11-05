using System.Net;
using System.Net.Http;
using System.Threading;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests.FundingProjection;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses.FundingProjection;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Services;

[TestFixture]
public class WhenCallingEmployerFinanceJobsOuterService
{
    private Mock<IEmployerFinanceJobsOuterApiClient> _apiClient;
    private EmployerFinanceJobsOuterService _service;

    [SetUp]
    public void Setup()
    {
        _apiClient = new Mock<IEmployerFinanceJobsOuterApiClient>();
        _service = new EmployerFinanceJobsOuterService(_apiClient.Object, Mock.Of<ILogger<EmployerFinanceJobsOuterService>>());
    }

    [Test]
    public async Task ImportCommittedLearnersAsync_WhenCalled_ShouldPostAccountRequest()
    {
        // Arrange
        var response = new ImportCommittedLearnersResponse { IsSuccessful = true };
        SetupImport(new ApiResponse<ImportCommittedLearnersResponse>(response, HttpStatusCode.OK, string.Empty));

        // Act
        var result = await _service.ImportCommittedLearnersAsync(12345, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(response);
        _apiClient.Verify(client => client.PostWithResponseCodeAsync<ImportCommittedLearnersResponse>(
            It.Is<ImportCommittedLearnersRequest>(request => request.Payload.AccountId == 12345),
            CancellationToken.None), Times.Once);
    }

    [TestCase(HttpStatusCode.NotFound)]
    [TestCase(HttpStatusCode.InternalServerError)]
    public async Task ImportCommittedLearnersAsync_WhenApiFails_ShouldThrow(HttpStatusCode statusCode)
    {
        // Arrange
        SetupImport(new ApiResponse<ImportCommittedLearnersResponse>(null, statusCode, "error"));

        // Act
        var act = () => _service.ImportCommittedLearnersAsync(12345, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), CancellationToken.None);

        // Assert
        (await act.Should().ThrowAsync<HttpRequestException>()).Which.StatusCode.Should().Be(statusCode);
    }

    [Test]
    public async Task ImportCommittedLearnersAsync_WhenResponseIsEmpty_ShouldThrow()
    {
        // Arrange
        SetupImport(new ApiResponse<ImportCommittedLearnersResponse>(null, HttpStatusCode.OK, string.Empty));

        // Act
        var act = () => _service.ImportCommittedLearnersAsync(12345, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Test]
    public async Task PostJobAsync_WhenCalled_ShouldPostJobRequest()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var payload = CreateJobPayload();
        SetupPostJob(new ApiResponse<object>(null, HttpStatusCode.OK, string.Empty));

        // Act
        await _service.PostJobAsync(jobId, payload, CancellationToken.None);

        // Assert
        _apiClient.Verify(client => client.PostWithResponseCodeAsync<object>(
            It.Is<PostJobRequest>(request => request.JobId == jobId && request.Payload == payload),
            CancellationToken.None), Times.Once);
    }

    [Test]
    public async Task PostJobAsync_WhenApiFails_ShouldThrow()
    {
        // Arrange
        SetupPostJob(new ApiResponse<object>(null, HttpStatusCode.NotFound, string.Empty));

        // Act
        var act = () => _service.PostJobAsync(Guid.NewGuid(), CreateJobPayload(), CancellationToken.None);

        // Assert
        (await act.Should().ThrowAsync<HttpRequestException>()).Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private void SetupImport(ApiResponse<ImportCommittedLearnersResponse> response) =>
        _apiClient
            .Setup(client => client.PostWithResponseCodeAsync<ImportCommittedLearnersResponse>(
                It.IsAny<ImportCommittedLearnersRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

    private void SetupPostJob(ApiResponse<object> response) =>
        _apiClient
            .Setup(client => client.PostWithResponseCodeAsync<object>(It.IsAny<PostJobRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

    private static PostJobRequestData CreateJobPayload() =>
        new()
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
}
