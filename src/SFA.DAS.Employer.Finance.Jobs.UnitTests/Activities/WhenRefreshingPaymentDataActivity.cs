using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Functions.Activities;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Provider.Events.Api.Types;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.Activities;

[TestFixture]
public class WhenRefreshingPaymentDataActivity
{
    private Mock<ILogger<RefreshPaymentDataActivities>> _loggerMock;
    private Mock<IRefreshPaymentDataService> _refreshPaymentDataServiceMock;
    private RefreshPaymentDataActivities _activity;

    [SetUp]
    public void SetUp()
    {
        _loggerMock = new Mock<ILogger<RefreshPaymentDataActivities>>();
        _refreshPaymentDataServiceMock = new Mock<IRefreshPaymentDataService>();
        _activity = new RefreshPaymentDataActivities(_loggerMock.Object, _refreshPaymentDataServiceMock.Object);
    }

    [Test]
    public async Task Then_Returns_Only_Newly_Filtered_Payments_For_Downstream_Processing()
    {
        var paymentToStage = new Payment { Id = Guid.NewGuid().ToString() };
        var existingPayment = new Payment { Id = Guid.NewGuid().ToString() };
        var input = new RefreshPaymentDataInput
        {
            AccountId = 12345,
            CorrelationId = "correlation-id",
            Payments = [paymentToStage, existingPayment],
            PaymentIds = [existingPayment.Id!]
        };

        _refreshPaymentDataServiceMock
            .Setup(service => service.FilterPayments(input.Payments, input.PaymentIds, input.AccountId, input.CorrelationId))
            .Returns([
                new PaymentStaging
                {
                    PaymentId = Guid.Parse(paymentToStage.Id!)
                }
            ]);
        _refreshPaymentDataServiceMock
            .Setup(service => service.PostPaymentsToStaging(It.IsAny<List<PaymentStaging>>(), input.CorrelationId))
            .ReturnsAsync(new RefreshPaymentDataResult
            {
                PaymentsCreated = 1,
                Status = "Succeeded",
                Message = "ok"
            });

        var result = await _activity.RefreshPaymentDataActivity(input);

        result.PaymentsCreated.Should().Be(1);
        result.Status.Should().Be("Succeeded");
        result.PaymentDetails.Should().HaveCount(1);
        result.PaymentDetails.Single().Id.Should().Be(paymentToStage.Id);
    }

    [Test]
    public async Task Then_Skips_Posting_When_No_New_Payments_Are_Found()
    {
        var input = new RefreshPaymentDataInput
        {
            AccountId = 12345,
            CorrelationId = "correlation-id",
            Payments = [new Payment { Id = Guid.NewGuid().ToString() }],
            PaymentIds = []
        };

        _refreshPaymentDataServiceMock
            .Setup(service => service.FilterPayments(input.Payments, input.PaymentIds, input.AccountId, input.CorrelationId))
            .Returns([]);

        var result = await _activity.RefreshPaymentDataActivity(input);

        result.PaymentsCreated.Should().Be(0);
        result.PaymentDetails.Should().BeEmpty();
        _refreshPaymentDataServiceMock.Verify(
            service => service.PostPaymentsToStaging(It.IsAny<List<PaymentStaging>>(), It.IsAny<string>()),
            Times.Never);
    }

    [Test]
    public async Task Then_Returns_Failed_And_Does_Not_Return_Payment_Details_When_Posting_To_Staging_Fails()
    {
        var paymentToStage = new Payment { Id = Guid.NewGuid().ToString() };
        var input = new RefreshPaymentDataInput
        {
            AccountId = 14331,
            CorrelationId = "correlation-id",
            Payments = [paymentToStage],
            PaymentIds = []
        };

        _refreshPaymentDataServiceMock
            .Setup(service => service.FilterPayments(input.Payments, input.PaymentIds, input.AccountId, input.CorrelationId))
            .Returns([
                new PaymentStaging
                {
                    PaymentId = Guid.Parse(paymentToStage.Id!)
                }
            ]);
        _refreshPaymentDataServiceMock
            .Setup(service => service.PostPaymentsToStaging(It.IsAny<List<PaymentStaging>>(), input.CorrelationId))
            .ReturnsAsync(new RefreshPaymentDataResult
            {
                PaymentsCreated = 0,
                Status = "Failed",
                Message = "Finance API returned BadRequest"
            });

        var result = await _activity.RefreshPaymentDataActivity(input);

        result.PaymentsCreated.Should().Be(0);
        result.Status.Should().Be("Failed");
        result.Message.Should().Be("Finance API returned BadRequest");
        result.PaymentDetails.Should().BeEmpty();
    }
}
