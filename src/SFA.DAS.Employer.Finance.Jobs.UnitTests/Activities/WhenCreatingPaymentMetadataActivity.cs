using System.Threading;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Functions.Activities;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Provider.Events.Api.Types;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Activities;

[TestFixture]
public class WhenCreatingPaymentMetadataActivity
{
    private Mock<ILogger<PaymentMetadataActivities>> _loggerMock;
    private Mock<IServiceProvider> _serviceProviderMock;
    private Mock<IPaymentMetadataService> _paymentMetadataServiceMock;
    private PaymentMetadataActivities _activity;

    [SetUp]
    public void SetUp()
    {
        _loggerMock = new Mock<ILogger<PaymentMetadataActivities>>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _paymentMetadataServiceMock = new Mock<IPaymentMetadataService>();
        _activity = new PaymentMetadataActivities(_loggerMock.Object, _serviceProviderMock.Object);
    }

    [Test]
    public async Task Then_Returns_Metadata_Service_Result()
    {
        var input = new CreatePaymentMetadataInput
        {
            AccountId = 12345,
            CorrelationId = "correlation-id",
            PaymentDetails = [new Payment { Id = Guid.NewGuid().ToString() }]
        };
        var expectedResult = new CreatePaymentMetadataResult
        {
            MetadataCreated = 1,
            Status = "Succeeded",
            Message = "ok"
        };

        _serviceProviderMock
            .Setup(provider => provider.GetService(typeof(IPaymentMetadataService)))
            .Returns(_paymentMetadataServiceMock.Object);
        _paymentMetadataServiceMock
            .Setup(service => service.CreatePaymentMetadata(input, CancellationToken.None))
            .ReturnsAsync(expectedResult);

        var result = await _activity.CreatePaymentMetadataActivity(input);

        result.Should().BeSameAs(expectedResult);
    }

    [Test]
    public async Task Then_Returns_Failed_Result_When_Metadata_Service_Configuration_Is_Invalid()
    {
        var input = new CreatePaymentMetadataInput
        {
            AccountId = 12345,
            CorrelationId = "correlation-id",
            PaymentDetails = [new Payment { Id = Guid.NewGuid().ToString() }]
        };

        _serviceProviderMock
            .Setup(provider => provider.GetService(typeof(IPaymentMetadataService)))
            .Throws(new UriFormatException("Invalid URI: The URI is empty."));

        var result = await _activity.CreatePaymentMetadataActivity(input);

        result.MetadataCreated.Should().Be(0);
        result.Status.Should().Be("Failed");
        result.Message.Should().Be("Invalid URI: The URI is empty.");
        _paymentMetadataServiceMock.Verify(
            service => service.CreatePaymentMetadata(It.IsAny<CreatePaymentMetadataInput>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
