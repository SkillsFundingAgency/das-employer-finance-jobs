using FluentAssertions;
using HMRC.ESFA.Levy.Api.Types;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Activities;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces.HMRC;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.UnitTests.Activities;

[TestFixture]
public class ImportLevyDeclarationsActivityTests
{
    private Mock<IHmrcService> _hmrcService = null!;
    private Mock<ILogger<ImportLevyDeclarationsActivity>> _logger = null!;
    private Mock<FunctionContext> _functionContext = null!;
    private ImportLevyDeclarationsActivity _activity = null!;

    [SetUp]
    public void SetUp()
    {
        _hmrcService = new Mock<IHmrcService>();
        _logger = new Mock<ILogger<ImportLevyDeclarationsActivity>>();
        _functionContext = new Mock<FunctionContext>();
        _activity = new ImportLevyDeclarationsActivity(_hmrcService.Object, _logger.Object);
    }

    [Test]
    public async Task Run_Calls_HmrcService_With_Request_Values()
    {
        var cancellationToken = new CancellationTokenSource().Token;
        _functionContext.SetupGet(x => x.CancellationToken).Returns(cancellationToken);

        var fromDate = new DateTime(2026, 1, 1);
        var request = new ImportLevyActivityRequest("123/AB12345", fromDate, "corr-123");
        var levyDeclarations = new LevyDeclarations
        {
            Declarations = []
        };

        _hmrcService
            .Setup(x => x.GetLevyDeclarations(request.EmpRef, request.FromDate, request.CorrelationId, cancellationToken))
            .ReturnsAsync(levyDeclarations);

        var result = await _activity.Run(request, _functionContext.Object);

        result.EmpRef.Should().Be(request.EmpRef);
        result.FromDate.Should().Be(fromDate);
        result.DeclarationsCount.Should().Be(0);
        result.LevyDeclarations.Should().BeSameAs(levyDeclarations);

        _hmrcService.Verify(
            x => x.GetLevyDeclarations(request.EmpRef, request.FromDate, request.CorrelationId, cancellationToken),
            Times.Once);
    }

    [Test]
    public async Task Run_Returns_Empty_LevyDeclarations_When_Service_Returns_Null()
    {
        var cancellationToken = new CancellationTokenSource().Token;
        _functionContext.SetupGet(x => x.CancellationToken).Returns(cancellationToken);

        var request = new ImportLevyActivityRequest("123/AB12345", null, "corr-123");

        _hmrcService
            .Setup(x => x.GetLevyDeclarations(request.EmpRef, request.FromDate, request.CorrelationId, cancellationToken))
            .ReturnsAsync((LevyDeclarations?)null);

        var result = await _activity.Run(request, _functionContext.Object);

        result.EmpRef.Should().Be(request.EmpRef);
        result.FromDate.Should().BeNull();
        result.DeclarationsCount.Should().Be(0);
        result.LevyDeclarations.Should().NotBeNull();
    }
}
