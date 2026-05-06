<<<<<<< HEAD
using HMRC.ESFA.Levy.Api.Types;
=======
>>>>>>> 8ec3ca5367ae1ad2a7507a98b45f20b5f7ab141c
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Models;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Services;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.UnitTests.Services;

[TestFixture]
public class LevyDeclarationNormalizerTests
{
    private LevyDeclarationNormalizer _normalizer = null!;

    [SetUp]
    public void SetUp()
    {
        _normalizer = new LevyDeclarationNormalizer(Mock.Of<ILogger<LevyDeclarationNormalizer>>());
    }

    [Test]
    public void Normalize_MapsHmrcDeclarations_AndReturnsDeterministicOrder()
    {
        var input = CreateInput(
            CreateHmrcDeclaration("declaration-b", 2, new DateTime(2025, 6, 2), levyDueYearToDate: 200),
            CreateHmrcDeclaration("declaration-a", 1, new DateTime(2025, 5, 1), levyDueYearToDate: 100));

        var result = _normalizer.Normalize(input);

        result.AccountId.Should().Be(12345);
        result.EmpRef.Should().Be("123/AB456");
        result.SourceDeclarationCount.Should().Be(2);
        result.Declarations.Select(x => x.Id).Should().Equal("declaration-a", "declaration-b");
        result.Declarations[0].SubmissionId.Should().Be(1);
        result.Declarations[0].LevyDueYtd.Should().Be(100);
        result.Declarations[0].PayrollYear.Should().Be("25-26");
        result.Declarations[0].PayrollMonth.Should().Be(1);
<<<<<<< HEAD
        result.Declarations[0].SubmissionType.Should().Be(LevyDeclarationSubmissionStatus.LatestSubmission.ToString());
=======
        result.Declarations[0].SubmissionType.Should().Be("FullPaymentSubmission");
>>>>>>> 8ec3ca5367ae1ad2a7507a98b45f20b5f7ab141c
        result.Declarations[0].LevyAllowanceForFullYear.Should().Be(15000);
    }

    [Test]
    public void Normalize_FiltersDuplicateHmrcDeclarations_KeepingTheEarliestSubmission()
    {
        var input = CreateInput(
            CreateHmrcDeclaration("duplicate-id", 2, new DateTime(2025, 5, 2), levyDueYearToDate: 200),
            CreateHmrcDeclaration("duplicate-id", 1, new DateTime(2025, 5, 1), levyDueYearToDate: 100));

        var result = _normalizer.Normalize(input);

        result.DuplicateDeclarationCount.Should().Be(1);
        result.Declarations.Should().ContainSingle();
        result.Declarations[0].SubmissionId.Should().Be(1);
        result.Declarations[0].LevyDueYtd.Should().Be(100);
    }

    [Test]
    public void Normalize_FiltersExistingDeclarations_BeforePersistence()
    {
        var input = CreateInput(
            CreateHmrcDeclaration("existing-id", 1, new DateTime(2025, 5, 1)),
            CreateHmrcDeclaration("new-id", 2, new DateTime(2025, 5, 2)));
        input.ExistingSubmissionIds = ["existing-id"];

        var result = _normalizer.Normalize(input);

        result.ExistingDeclarationCount.Should().Be(1);
        result.Declarations.Should().ContainSingle();
        result.Declarations[0].Id.Should().Be("new-id");
    }

    [Test]
    public void Normalize_FiltersPreLevyDeclarations()
    {
        var input = CreateInput(
            CreateHmrcDeclaration("pre-levy", 1, new DateTime(2016, 5, 1), payrollYear: "16-17"),
            CreateHmrcDeclaration("post-levy", 2, new DateTime(2017, 5, 1), payrollYear: "17-18"));

        var result = _normalizer.Normalize(input);

        result.PreLevyDeclarationCount.Should().Be(1);
        result.Declarations.Should().ContainSingle();
        result.Declarations[0].Id.Should().Be("post-levy");
    }

    [Test]
    public void Normalize_FiltersFuturePeriodDeclarations()
    {
        var input = CreateInput(
            CreateHmrcDeclaration("future-period", 1, new DateTime(2026, 3, 25), payrollYear: "25-26", payrollMonth: 12),
            CreateHmrcDeclaration("current-period", 2, new DateTime(2025, 4, 25), payrollYear: "25-26", payrollMonth: 1));
        input.ProcessingDate = new DateTime(2026, 4, 1);

        var result = _normalizer.Normalize(input);

        result.FutureDeclarationCount.Should().Be(1);
        result.Declarations.Should().ContainSingle();
        result.Declarations[0].Id.Should().Be("current-period");
    }

    [Test]
    public void Normalize_ClearsLevyDueYtd_ForNoPaymentDeclarations()
    {
        var input = CreateInput(
            CreateHmrcDeclaration("no-payment", 1, new DateTime(2025, 5, 1), levyDueYearToDate: 123.45m, noPaymentForPeriod: true));

        var result = _normalizer.Normalize(input);

        result.Declarations.Should().ContainSingle();
        result.Declarations[0].NoPaymentForPeriod.Should().BeTrue();
        result.Declarations[0].LevyDueYtd.Should().BeNull();
    }

    [Test]
    public void Normalize_FlagsEndOfYearAdjustment_AndCalculatesAmountUsingCurrentFeed()
    {
        var input = CreateInput(
            CreateHmrcDeclaration("period-12", 1, new DateTime(2026, 4, 1), payrollYear: "25-26", payrollMonth: 12, levyDueYearToDate: 100),
            CreateHmrcDeclaration("year-end-adjustment", 2, new DateTime(2026, 4, 25), payrollYear: "25-26", payrollMonth: 12, levyDueYearToDate: 150));

        var result = _normalizer.Normalize(input);

        var adjustment = result.Declarations.Single(x => x.Id == "year-end-adjustment");
        adjustment.EndOfYearAdjustment.Should().BeTrue();
        adjustment.EndOfYearAdjustmentAmount.Should().Be(-50);
    }

    [Test]
    public void Normalize_CalculatesEndOfYearAdjustmentAmount_WhenNoEffectivePeriod12DeclarationExists()
    {
        var input = CreateInput(
            CreateHmrcDeclaration("year-end-adjustment", 1, new DateTime(2026, 4, 25), payrollYear: "25-26", payrollMonth: 12, levyDueYearToDate: 150));

        var result = _normalizer.Normalize(input);

        var adjustment = result.Declarations.Single();
        adjustment.EndOfYearAdjustment.Should().BeTrue();
        adjustment.EndOfYearAdjustmentAmount.Should().Be(-150);
    }

    [Test]
    public void Normalize_LeavesNoPaymentEndOfYearAdjustmentAmountAsZero()
    {
        var input = CreateInput(
            CreateHmrcDeclaration(
                "no-payment-year-end-adjustment",
                1,
                new DateTime(2026, 4, 25),
                payrollYear: "25-26",
                payrollMonth: 12,
                levyDueYearToDate: 150,
                noPaymentForPeriod: true));

        var result = _normalizer.Normalize(input);

        var adjustment = result.Declarations.Single();
        adjustment.EndOfYearAdjustment.Should().BeTrue();
        adjustment.LevyDueYtd.Should().BeNull();
        adjustment.EndOfYearAdjustmentAmount.Should().Be(0);
    }

    [Test]
    public void Normalize_UsesExistingPeriod12Placeholder_WhenCurrentFeedDoesNotContainEffectivePeriod12Declaration()
    {
        var input = CreateInput(
            CreateHmrcDeclaration("year-end-adjustment", 1, new DateTime(2026, 4, 25), payrollYear: "25-26", payrollMonth: 12, levyDueYearToDate: 300));
        input.ExistingPeriod12Declarations =
        [
            new NormalizedLevyDeclaration
            {
                Id = "existing-period-12",
                PayrollYear = "25-26",
                PayrollMonth = 12,
                SubmissionDate = new DateTime(2026, 4, 1),
                LevyDueYtd = 250
            }
        ];

        var result = _normalizer.Normalize(input);

        var adjustment = result.Declarations.Single();
        adjustment.EndOfYearAdjustmentAmount.Should().Be(-50);
    }

    [Test]
    public void Normalize_Throws_WhenInputIsNull()
    {
        Action act = () => _normalizer.Normalize(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Normalize_Throws_WhenProcessingDateIsNotSupplied()
    {
        var input = CreateInput(CreateHmrcDeclaration("valid", 1, new DateTime(2025, 5, 1)));
        input.ProcessingDate = default;

        Action act = () => _normalizer.Normalize(input);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ProcessingDate*");
    }

    [Test]
<<<<<<< HEAD
=======
    public void Normalize_Throws_WhenEndOfYearAdjustmentHasNoLevyDueYtd_AndIsNotNoPayment()
    {
        var input = CreateInput(
            CreateHmrcDeclaration("invalid-adjustment", 1, new DateTime(2026, 4, 25), payrollYear: "25-26", payrollMonth: 12, levyDueYearToDate: null));

        Action act = () => _normalizer.Normalize(input);

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
>>>>>>> 8ec3ca5367ae1ad2a7507a98b45f20b5f7ab141c
    public void Normalize_Throws_WhenPayrollMonthIsInvalid()
    {
        var input = CreateInput(
            CreateHmrcDeclaration("invalid-month", 1, new DateTime(2025, 5, 1), payrollMonth: 13));

        Action act = () => _normalizer.Normalize(input);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Payroll month must be between 1 and 12*");
    }

<<<<<<< HEAD
    private static NormalizeLevyDeclarationsInput CreateInput(params Declaration[] declarations)
=======
    private static NormalizeLevyDeclarationsInput CreateInput(params HmrcLevyDeclaration[] declarations)
>>>>>>> 8ec3ca5367ae1ad2a7507a98b45f20b5f7ab141c
    {
        return new NormalizeLevyDeclarationsInput
        {
            CorrelationId = "corr-123",
            AccountId = 12345,
            EmpRef = "123/AB456",
            ProcessingDate = new DateTime(2030, 1, 1),
            HmrcDeclarations = declarations.ToList()
        };
    }

<<<<<<< HEAD
    private static Declaration CreateHmrcDeclaration(
=======
    private static HmrcLevyDeclaration CreateHmrcDeclaration(
>>>>>>> 8ec3ca5367ae1ad2a7507a98b45f20b5f7ab141c
        string id,
        long submissionId,
        DateTime submissionTime,
        string payrollYear = "25-26",
        short payrollMonth = 1,
<<<<<<< HEAD
        decimal levyDueYearToDate = 100,
        bool noPaymentForPeriod = false)
    {
        return new Declaration
=======
        decimal? levyDueYearToDate = 100,
        bool noPaymentForPeriod = false)
    {
        return new HmrcLevyDeclaration
>>>>>>> 8ec3ca5367ae1ad2a7507a98b45f20b5f7ab141c
        {
            Id = id,
            SubmissionId = submissionId,
            SubmissionTime = submissionTime,
<<<<<<< HEAD
            LevyDeclarationSubmissionStatus = LevyDeclarationSubmissionStatus.LatestSubmission,
            LevyAllowanceForFullYear = 15000,
            LevyDueYearToDate = levyDueYearToDate,
            NoPaymentForPeriod = noPaymentForPeriod,
            PayrollPeriod = new PayrollPeriod
=======
            SubmissionType = "FullPaymentSubmission",
            LevyAllowanceForFullYear = 15000,
            LevyDueYearToDate = levyDueYearToDate,
            NoPaymentForPeriod = noPaymentForPeriod,
            PayrollPeriod = new HmrcPayrollPeriod
>>>>>>> 8ec3ca5367ae1ad2a7507a98b45f20b5f7ab141c
            {
                Year = payrollYear,
                Month = payrollMonth
            }
        };
    }
}
