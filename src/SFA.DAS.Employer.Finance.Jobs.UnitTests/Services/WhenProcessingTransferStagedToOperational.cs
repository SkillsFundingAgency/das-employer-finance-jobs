using System.Net;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Services;

[TestFixture]
public class WhenProcessingTransferStagedToOperational
{
    private Mock<IFinanceApiClient<FinanceApiConfiguration>> _financeApiClientMock;
    private Mock<ILogger<TransferStagedToOperationalService>> _loggerMock;

    [SetUp]
    public void SetUp()
    {
        _financeApiClientMock = new Mock<IFinanceApiClient<FinanceApiConfiguration>>();
        _loggerMock = new Mock<ILogger<TransferStagedToOperationalService>>();
    }

    [Test]
    public async Task Then_Default_Off_Skips_Processing_And_Does_Not_Call_Finance_Api()
    {
        var service = CreateService(new ImportPaymentsOptions());

        var result = await service.Process(CreateInput());

        result.Status.Should().Be("Skipped");
        result.TransfersProcessed.Should().Be(0);
        result.Message.Should().Contain("processing is disabled");
        _financeApiClientMock.Verify(
            client => client.PostWithResponseCode<PostTransferStagedToOperationalResponse>(It.IsAny<PostTransferStagedToOperationalRequest>()),
            Times.Never);
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("Transfer staged-to-operational processing is disabled")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Test]
    public async Task Then_Calls_Finance_Api_When_Processing_Is_Enabled()
    {
        IApiRequest postedRequest = null;
        var service = CreateService(new ImportPaymentsOptions
        {
            TransferStagedToOperationalProcessingEnabled = true
        });

        _financeApiClientMock
            .Setup(client => client.PostWithResponseCode<PostTransferStagedToOperationalResponse>(
                It.IsAny<PostTransferStagedToOperationalRequest>()))
            .Callback<IApiRequest>(request => postedRequest = request)
            .ReturnsAsync(new ApiResponse<PostTransferStagedToOperationalResponse>(
                new PostTransferStagedToOperationalResponse
                {
                    ProcessedCount = 3,
                    Message = "ok"
                },
                HttpStatusCode.Created,
                null));

        var result = await service.Process(CreateInput());

        result.Status.Should().Be("Succeeded");
        result.TransfersProcessed.Should().Be(3);
        postedRequest.Should().BeOfType<PostTransferStagedToOperationalRequest>();
        postedRequest.GetUrl.Should().Be("api/staging/staged-to-operational");
        postedRequest.Data.Should().BeOfType<TransferStagedToOperationalRequest>();
        var requestModel = (TransferStagedToOperationalRequest)postedRequest.Data;
        requestModel.AccountId.Should().Be(12345);
        requestModel.PeriodEnd.Should().Be("2024-01");
        requestModel.CorrelationId.Should().Be("correlation-id");
    }

    [Test]
    public async Task Then_Returns_Failed_When_Finance_Api_Rejects_Processing()
    {
        var service = CreateService(new ImportPaymentsOptions
        {
            TransferStagedToOperationalProcessingEnabled = true
        });

        _financeApiClientMock
            .Setup(client => client.PostWithResponseCode<PostTransferStagedToOperationalResponse>(
                It.IsAny<PostTransferStagedToOperationalRequest>()))
            .ReturnsAsync(new ApiResponse<PostTransferStagedToOperationalResponse>(
                new PostTransferStagedToOperationalResponse(),
                HttpStatusCode.BadRequest,
                "Endpoint is not available"));

        var result = await service.Process(CreateInput());

        result.Status.Should().Be("Failed");
        result.TransfersProcessed.Should().Be(0);
        result.Message.Should().Contain("BadRequest");
        result.Message.Should().Contain("Endpoint is not available");
    }

    private TransferStagedToOperationalService CreateService(ImportPaymentsOptions options)
    {
        return new TransferStagedToOperationalService(
            _financeApiClientMock.Object,
            options,
            _loggerMock.Object);
    }

    private static TransferStagedToOperationalInput CreateInput()
    {
        return new TransferStagedToOperationalInput
        {
            AccountId = 12345,
            PeriodEndRef = "2024-01",
            CorrelationId = "correlation-id"
        };
    }
}
