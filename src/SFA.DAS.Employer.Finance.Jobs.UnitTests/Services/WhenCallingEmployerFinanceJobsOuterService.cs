using System.Threading;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses.FundingProjection;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Services;

[TestFixture]
public class WhenCallingEmployerFinanceJobsOuterService
{
    private Mock<IEmployerFinanceJobsOuterApiClient> _apiClient;
    private Mock<ILogger<EmployerFinanceJobsOuterService>> _logger;
    private EmployerFinanceJobsOuterService _service;

    [SetUp]
    public void SetUp()
    {
        _apiClient = new Mock<IEmployerFinanceJobsOuterApiClient>();
        _logger = new Mock<ILogger<EmployerFinanceJobsOuterService>>();
        _service = new EmployerFinanceJobsOuterService(_apiClient.Object, _logger.Object);
    }

    [Test]
    public async Task Then_Imports_Committed_Learners_And_Returns_Api_Response()
    {
        // Arrange
        var expectedResponse = new ImportCommittedLearnersResponse { TotalRecords = 10, BatchesProcessed = 2 };
        _apiClient
            .Setup(client => client.Post<ImportCommittedLearnersResponse>(
                It.Is<IApiRequest>(request => request.GetUrl == "learners/import")))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _service.ImportCommittedLearnersAsync(CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);
        _apiClient.Verify(client => client.Post<ImportCommittedLearnersResponse>(
            It.Is<IApiRequest>(request => request.GetUrl == "learners/import")), Times.Once);
    }

    [Test]
    public async Task Then_Updating_Committed_Learner_Costs_Returns_Api_Response()
    {
        // Arrange
        var expectedResponse = new UpdateCommittedLearnersCostResponse { TotalRecords = 10, SuccessfulRecords = 9 };
        _apiClient
            .Setup(client => client.Post<UpdateCommittedLearnersCostResponse>(
                It.Is<IApiRequest>(request => request.GetUrl == "learners/update")))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _service.UpdateCommittedLearnersCostAsync(CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);
        _apiClient.Verify(client => client.Post<UpdateCommittedLearnersCostResponse>(
            It.Is<IApiRequest>(request => request.GetUrl == "learners/update")), Times.Once);
    }

    [Test]
    public async Task Then_Recalculating_Funding_Projection_Returns_Api_Response()
    {
        // Arrange
        var expectedResponse = new RecalculateFundingProjectionResponse { TotalRecordsProcessed = 10, TotalRecordsUpdated = 8 };
        _apiClient
            .Setup(client => client.Post<RecalculateFundingProjectionResponse>(
                It.Is<IApiRequest>(request => request.GetUrl == "funding-projection/re-calculate")))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _service.RecalculateFundingProjectionAsync(CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);
        _apiClient.Verify(client => client.Post<RecalculateFundingProjectionResponse>(
            It.Is<IApiRequest>(request => request.GetUrl == "funding-projection/re-calculate")), Times.Once);
    }

    [Test]
    public async Task Then_Import_Exception_Is_Rethrown()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Import failed");
        _apiClient
            .Setup(client => client.Post<ImportCommittedLearnersResponse>(It.IsAny<IApiRequest>()))
            .ThrowsAsync(expectedException);

        // Act
        Func<Task> act = () => _service.ImportCommittedLearnersAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Import failed");
    }

    [Test]
    public async Task Then_Update_Exception_Is_Rethrown()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Update failed");
        _apiClient
            .Setup(client => client.Post<UpdateCommittedLearnersCostResponse>(It.IsAny<IApiRequest>()))
            .ThrowsAsync(expectedException);

        // Act
        Func<Task> act = () => _service.UpdateCommittedLearnersCostAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Update failed");
    }

    [Test]
    public async Task Then_Recalculate_Exception_Is_Rethrown()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Recalculate failed");
        _apiClient
            .Setup(client => client.Post<RecalculateFundingProjectionResponse>(It.IsAny<IApiRequest>()))
            .ThrowsAsync(expectedException);

        // Act
        Func<Task> act = () => _service.RecalculateFundingProjectionAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Recalculate failed");
    }
}