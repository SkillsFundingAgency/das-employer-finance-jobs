using NUnit.Framework;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.InnerAPI.Requests;

namespace SFA.DAS.Employer.Finance.Jobs.UnitTests.InnerAPI.Requests;

public class WhenBuildingGetApiRequestUrls
{
    [Test]
    public void Then_GetPaymentPeriodEndsUrl_Is_Correct()
    {
        // Arrange & Act
        var request = new GetPaymentPeriodEndsRequest();

        // Assert
        Assert.That(request.GetUrl, Is.EqualTo("api/periodends"));
    }

    [Test]
    public void Then_GetFinancePeriodEndsUrl_Is_Correct()
    {
        // Arrange & Act
        var request = new GetFinancePeriodEndsRequest();

        // Assert
        Assert.That(request.GetUrl, Is.EqualTo("api/period-ends"));
    }
}