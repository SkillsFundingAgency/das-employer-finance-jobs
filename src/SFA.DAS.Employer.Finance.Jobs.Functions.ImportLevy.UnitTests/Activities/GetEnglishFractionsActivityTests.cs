using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Activities;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.UnitTests.Helpers;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.UnitTests.Activities;

[TestFixture]
public class GetEnglishFractionsActivityTests
{
    private Mock<IEnglishFractionsService> _englishFractionsService = null!;
    private Mock<ILogger<GetEnglishFractionsActivity>> _logger = null!;
    private GetEnglishFractionsActivity _activity = null!;

    [SetUp]
    public void SetUp()
    {
        _englishFractionsService = new Mock<IEnglishFractionsService>();
        _logger = new Mock<ILogger<GetEnglishFractionsActivity>>();
        _activity = new GetEnglishFractionsActivity(_englishFractionsService.Object, _logger.Object);
    }

    [Test]
    public async Task Run_Returns_The_Service_Result()
    {
        var input = new GetEnglishFractionsActivityInput
        {
            CorrelationId = "corr-123",
            EmployerReference = "123/AB456",
            LastStoredFractionCalculatedDate = new DateTime(2026, 4, 1)
        };

        var expected = new EnglishFractionsFetchResult
        {
            CorrelationId = "corr-123",
            EmployerReference = "123/AB456",
            UpdateRequired = true,
            Fractions =
            [
                new EnglishFraction
                {
                    EmployerReference = "123/AB456",
                    DateCalculated = new DateTime(2026, 4, 5),
                    Amount = 0.55m
                }
            ]
        };

        _englishFractionsService.Setup(x => x.GetEnglishFractionsAsync(input, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _activity.Run(input);

        result.Should().BeEquivalentTo(expected);
        _logger.VerifyLogContains("Checking HMRC English fractions");
        _logger.VerifyLogContains("completed");
    }
}
