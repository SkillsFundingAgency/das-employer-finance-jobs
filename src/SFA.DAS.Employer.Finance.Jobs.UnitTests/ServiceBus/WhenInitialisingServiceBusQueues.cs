using System.Threading;
using Azure;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Functions.FundingProjection.ServiceBus;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.ServiceBus;

public class WhenInitialisingServiceBusQueues
{
    private Mock<ServiceBusAdministrationClient> _adminClient;
    private ServiceBusQueueInitialiser _sut;

    [SetUp]
    public void Setup()
    {
        _adminClient = new Mock<ServiceBusAdministrationClient>();
        _sut = new ServiceBusQueueInitialiser(_adminClient.Object, Mock.Of<ILogger<ServiceBusQueueInitialiser>>());
    }

    [Test]
    public async Task Then_The_Funding_Projection_Queue_Is_Created_When_It_Does_Not_Exist()
    {
        SetupQueueExists(false);

        await _sut.StartAsync(CancellationToken.None);

        _adminClient.Verify(x => x.CreateQueueAsync(
            It.Is<CreateQueueOptions>(o => o.Name == QueueConstants.FundingProjectionQueue),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Then_The_Funding_Projection_Queue_Is_Not_Created_When_It_Already_Exists()
    {
        SetupQueueExists(true);

        await _sut.StartAsync(CancellationToken.None);

        _adminClient.Verify(x => x.CreateQueueAsync(It.IsAny<CreateQueueOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private void SetupQueueExists(bool exists)
    {
        _adminClient
            .Setup(x => x.QueueExistsAsync(QueueConstants.FundingProjectionQueue, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(exists, Mock.Of<Response>()));
    }
}
