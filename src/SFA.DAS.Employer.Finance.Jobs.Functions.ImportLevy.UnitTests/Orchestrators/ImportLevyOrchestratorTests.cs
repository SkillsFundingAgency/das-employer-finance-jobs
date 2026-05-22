using FluentAssertions;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Models;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Orchestrators;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.UnitTests.Orchestrators;

[TestFixture]
public class ImportLevyOrchestratorTests
{
    private Mock<ILogger<ImportLevyOrchestrator>> _logger = null!;
    private Mock<TaskOrchestrationContext> _context = null!;
    private ImportLevyOrchestrator _orchestrator = null!;

    [SetUp]
    public void SetUp()
    {
        _logger = new Mock<ILogger<ImportLevyOrchestrator>>();
        _context = new Mock<TaskOrchestrationContext>();
        _orchestrator = new ImportLevyOrchestrator(
            _logger.Object,
            Options.Create(new ImportLevyProcessingOptions { MaxConcurrentHmrcActivities = 5 }));
    }

    [Test]
    public async Task RunOrchestrator_Returns_Success_When_Accounts_Are_Retrieved()
    {
        var input = new ImportLevyInput
        {
            CorrelationId = "corr-123",
            TriggeredAt = DateTime.UtcNow
        };
        var accountIds = new List<long> { 10, 20, 30 };

        _context.Setup(c => c.GetInput<ImportLevyInput>()).Returns(input);
        SetupHappyPathActivities(accountIds);

        var result = await _orchestrator.RunOrchestrator(_context.Object);

        result.Success.Should().BeTrue();
        result.CorrelationId.Should().Be("corr-123");
        result.TotalAccountsCount.Should().Be(3);
        result.AccountIds.Should().Equal(accountIds);
        result.ErrorMessage.Should().BeEmpty();
    }

    [Test]
    public async Task RunOrchestrator_Returns_Success_With_Empty_List_When_Activity_Returns_Null()
    {
        var input = new ImportLevyInput
        {
            CorrelationId = "corr-456",
            TriggeredAt = DateTime.UtcNow
        };

        _context.Setup(c => c.GetInput<ImportLevyInput>()).Returns(input);
        SetupHappyPathActivities(null);

        var result = await _orchestrator.RunOrchestrator(_context.Object);

        result.Success.Should().BeTrue();
        result.CorrelationId.Should().Be("corr-456");
        result.TotalAccountsCount.Should().Be(0);
        result.AccountIds.Should().BeEmpty();
    }

    [Test]
    public async Task RunOrchestrator_Returns_Failure_When_Activity_Throws()
    {
        var input = new ImportLevyInput
        {
            CorrelationId = "corr-789",
            TriggeredAt = DateTime.UtcNow
        };

        _context.Setup(c => c.GetInput<ImportLevyInput>()).Returns(input);
        _context.Setup(c => c.CallActivityAsync<List<long>>(It.IsAny<TaskName>(), It.IsAny<string>(), It.IsAny<TaskOptions>()))
            .ThrowsAsync(new Exception("activity failed"));

        var result = await _orchestrator.RunOrchestrator(_context.Object);

        result.Success.Should().BeFalse();
        result.CorrelationId.Should().Be("corr-789");
        result.ErrorMessage.Should().Be("activity failed");
    }

    [Test]
    public async Task RunOrchestrator_Captures_Granular_Stage_Failure_With_Activity_Name()
    {
        var input = new ImportLevyInput
        {
            CorrelationId = "corr-stage-failure",
            TriggeredAt = DateTime.UtcNow
        };

        _context.Setup(c => c.GetInput<ImportLevyInput>()).Returns(input);
        SetupHappyPathActivities([10]);
        _context.Setup(c => c.CallActivityAsync<EnglishFractionsFetchResult>(
                It.Is<TaskName>(name => name.Name == "GetEnglishFractionsActivity"),
                It.IsAny<GetEnglishFractionsActivityInput>(),
                It.IsAny<TaskOptions>()))
            .ThrowsAsync(new Exception("hmrc fractions failed"));

        var result = await _orchestrator.RunOrchestrator(_context.Object);

        result.Success.Should().BeFalse();
        result.FailedItems.Should().ContainSingle(x => x.ActivityName == "GetEnglishFractionsActivity");
        result.RunSummary.GetEnglishFractionsRetries.Should().BeGreaterThan(0);
    }

    private void SetupHappyPathActivities(List<long>? accountIds)
    {
        _context.Setup(c => c.CallActivityAsync<List<long>>(It.IsAny<TaskName>(), It.IsAny<string>(), It.IsAny<TaskOptions>()))
            .ReturnsAsync(accountIds ?? []);

        _context.Setup(c => c.CallActivityAsync<List<PayeScheme>>(It.IsAny<TaskName>(), It.IsAny<GetPayeSchemesByAccountActivityRequest>(), It.IsAny<TaskOptions>()))
            .ReturnsAsync([new PayeScheme { EmpRef = "123/AB12345" }]);

        _context.Setup(c => c.CallActivityAsync<PayeScheme>(It.IsAny<TaskName>(), It.IsAny<GetLevyDeclarationLastSubmissionDateActivityRequest>(), It.IsAny<TaskOptions>()))
            .ReturnsAsync(new PayeScheme { EmpRef = "123/AB12345", LastSubmissionDate = new DateTime(2024, 1, 1) });

        _context.Setup(c => c.CallActivityAsync<ImportLevyDeclarationsActivityResult>(It.IsAny<TaskName>(), It.IsAny<ImportLevyActivityRequest>(), It.IsAny<TaskOptions>()))
            .ReturnsAsync(new ImportLevyDeclarationsActivityResult("123/AB12345", new DateTime(2024, 1, 1), 1, new HMRC.ESFA.Levy.Api.Types.LevyDeclarations()));

        _context.Setup(c => c.CallActivityAsync<DateTime?>(It.IsAny<TaskName>(), It.IsAny<GetLastEnglishFractionCalculatedDateActivityRequest>(), It.IsAny<TaskOptions>()))
            .ReturnsAsync(new DateTime(2024, 1, 31));

        _context.Setup(c => c.CallActivityAsync<EnglishFractionsFetchResult>(It.IsAny<TaskName>(), It.IsAny<GetEnglishFractionsActivityInput>(), It.IsAny<TaskOptions>()))
            .ReturnsAsync(new EnglishFractionsFetchResult
            {
                CorrelationId = "corr",
                EmployerReference = "123/AB12345",
                UpdateRequired = true,
                HmrcLatestUpdateDate = new DateTime(2024, 2, 1),
                Fractions = []
            });

        _context.Setup(c => c.CallActivityAsync<EnglishFractionsPersistenceResult>(It.IsAny<TaskName>(), It.IsAny<EnglishFractionsFetchResult>(), It.IsAny<TaskOptions>()))
            .ReturnsAsync(new EnglishFractionsPersistenceResult
            {
                CorrelationId = "corr",
                EmployerReference = "123/AB12345",
                UpdateRequired = true
            });

        _context.Setup(c => c.CallActivityAsync<EnglishFractionCalculationDatePersistenceResult>(It.IsAny<TaskName>(), It.IsAny<EnglishFractionsFetchResult>(), It.IsAny<TaskOptions>()))
            .ReturnsAsync(new EnglishFractionCalculationDatePersistenceResult
            {
                CorrelationId = "corr",
                DateCalculated = new DateTime(2024, 2, 1),
                Persisted = true,
                UpdateRequired = true
            });

        _context.Setup(c => c.CallActivityAsync<List<string>>(It.IsAny<TaskName>(), It.IsAny<GetExistingLevySubmissionIdsActivityRequest>(), It.IsAny<TaskOptions>()))
            .ReturnsAsync([]);

        _context.Setup(c => c.CallActivityAsync<List<NormalizedLevyDeclaration>>(It.IsAny<TaskName>(), It.IsAny<GetExistingPeriod12LevyDeclarationsActivityRequest>(), It.IsAny<TaskOptions>()))
            .ReturnsAsync([]);

        _context.Setup(c => c.CallActivityAsync<NormalizeLevyDeclarationsResult>(It.IsAny<TaskName>(), It.IsAny<NormalizeLevyDeclarationsInput>(), It.IsAny<TaskOptions>()))
            .ReturnsAsync(new NormalizeLevyDeclarationsResult
            {
                CorrelationId = "corr",
                EmpRef = "123/AB12345",
                AccountId = 10,
                Declarations = []
            });

        _context.Setup(c => c.CallActivityAsync<SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Models.PersistLevyDeclarationsActivityResult>(It.IsAny<TaskName>(), It.IsAny<NormalizeLevyDeclarationsResult>(), It.IsAny<TaskOptions>()))
            .ReturnsAsync(new SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Models.PersistLevyDeclarationsActivityResult
            {
                CorrelationId = "corr",
                AccountId = 10,
                EmpRef = "123/AB12345",
                Success = true,
                DeclarationsSubmitted = 1,
                DeclarationsPersisted = 1,
                TransactionsCreated = 1
            });
    }
}

