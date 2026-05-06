<<<<<<< HEAD
using HMRC.ESFA.Levy.Api.Types;
=======
>>>>>>> 8ec3ca5367ae1ad2a7507a98b45f20b5f7ab141c
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Activities;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Models;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Services;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.UnitTests.Activities;

[TestFixture]
public class NormalizeLevyDeclarationsActivityTests
{
    private Mock<ILevyDeclarationNormalizer> _normalizer = null!;
    private NormalizeLevyDeclarationsActivity _activity = null!;

    [SetUp]
    public void SetUp()
    {
        _normalizer = new Mock<ILevyDeclarationNormalizer>();
        _activity = new NormalizeLevyDeclarationsActivity(
            _normalizer.Object,
            Mock.Of<ILogger<NormalizeLevyDeclarationsActivity>>());
    }

    [Test]
    public void Run_ReturnsNormalizerResult()
    {
        var input = new NormalizeLevyDeclarationsInput
        {
            CorrelationId = "corr-123",
            AccountId = 12345,
            EmpRef = "123/AB456",
            ProcessingDate = new DateTime(2026, 4, 28),
            HmrcDeclarations =
            [
<<<<<<< HEAD
                new Declaration
=======
                new HmrcLevyDeclaration
>>>>>>> 8ec3ca5367ae1ad2a7507a98b45f20b5f7ab141c
                {
                    Id = "declaration-1"
                }
            ]
        };
        var expectedResult = new NormalizeLevyDeclarationsResult
        {
            CorrelationId = input.CorrelationId,
            AccountId = input.AccountId,
            EmpRef = input.EmpRef,
            Declarations =
            [
                new NormalizedLevyDeclaration
                {
                    Id = "declaration-1"
                }
            ]
        };

        _normalizer
            .Setup(x => x.Normalize(input))
            .Returns(expectedResult);

        var result = _activity.Run(input);

        result.Should().BeSameAs(expectedResult);
        _normalizer.Verify(x => x.Normalize(input), Times.Once);
    }

    [Test]
    public void Run_Throws_WhenInputIsNull()
    {
        Action act = () => _activity.Run(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
