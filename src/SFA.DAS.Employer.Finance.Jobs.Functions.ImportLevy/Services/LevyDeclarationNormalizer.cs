<<<<<<< HEAD
using HMRC.ESFA.Levy.Api.Types;
=======
>>>>>>> 8ec3ca5367ae1ad2a7507a98b45f20b5f7ab141c
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Services;

public class LevyDeclarationNormalizer(ILogger<LevyDeclarationNormalizer> logger) : ILevyDeclarationNormalizer
{
    public NormalizeLevyDeclarationsResult Normalize(NormalizeLevyDeclarationsInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.ProcessingDate == default)
        {
            throw new InvalidOperationException("ProcessingDate must be supplied by the orchestrator to keep levy normalization replay-safe.");
        }

        var sourceDeclarations = input.HmrcDeclarations ?? [];
        var declarations = sourceDeclarations
            .Select(NormalizeDeclaration)
            .OrderBy(x => x.SubmissionDate)
            .ThenBy(x => x.Id, StringComparer.Ordinal)
            .ThenBy(x => x.SubmissionId)
            .ToList();

        var sourceDeclarationCount = declarations.Count;

        declarations = FilterDuplicateHmrcDeclarations(input, declarations, out var duplicateDeclarationCount);
        declarations = FilterExistingDeclarations(input, declarations, out var existingDeclarationCount);
        declarations = FilterPreLevyDeclarations(input, declarations, out var preLevyDeclarationCount);
        declarations = FilterFutureDeclarations(input, declarations, out var futureDeclarationCount);

        ProcessNoPaymentForPeriodDeclarations(declarations);
        SetEndOfYearAdjustmentProperties(declarations);
        ProcessEndOfYearAdjustmentDeclarations(input, declarations);

        return new NormalizeLevyDeclarationsResult
        {
            CorrelationId = input.CorrelationId,
            AccountId = input.AccountId,
            EmpRef = input.EmpRef,
            Declarations = declarations,
            SourceDeclarationCount = sourceDeclarationCount,
            DuplicateDeclarationCount = duplicateDeclarationCount,
            ExistingDeclarationCount = existingDeclarationCount,
            FutureDeclarationCount = futureDeclarationCount,
            PreLevyDeclarationCount = preLevyDeclarationCount
        };
    }

<<<<<<< HEAD
    private static NormalizedLevyDeclaration NormalizeDeclaration(Declaration declaration)
=======
    private static NormalizedLevyDeclaration NormalizeDeclaration(HmrcLevyDeclaration declaration)
>>>>>>> 8ec3ca5367ae1ad2a7507a98b45f20b5f7ab141c
    {
        return new NormalizedLevyDeclaration
        {
            Id = declaration.Id,
            SubmissionId = declaration.SubmissionId,
            LevyDueYtd = declaration.LevyDueYearToDate,
            SubmissionDate = declaration.SubmissionTime,
<<<<<<< HEAD
            SubmissionType = declaration.LevyDeclarationSubmissionStatus.ToString(),
=======
            SubmissionType = declaration.SubmissionType,
>>>>>>> 8ec3ca5367ae1ad2a7507a98b45f20b5f7ab141c
            LevyAllowanceForFullYear = declaration.LevyAllowanceForFullYear,
            PayrollYear = declaration.PayrollPeriod?.Year ?? string.Empty,
            PayrollMonth = declaration.PayrollPeriod?.Month,
            NoPaymentForPeriod = declaration.NoPaymentForPeriod,
            DateCeased = declaration.DateCeased,
            InactiveFrom = declaration.InactiveFrom,
            InactiveTo = declaration.InactiveTo
        };
    }

    private List<NormalizedLevyDeclaration> FilterDuplicateHmrcDeclarations(
        NormalizeLevyDeclarationsInput input,
        List<NormalizedLevyDeclaration> declarations,
        out int duplicateDeclarationCount)
    {
        var duplicateIds = declarations
            .GroupBy(x => x.Id, StringComparer.Ordinal)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToList();

        duplicateDeclarationCount = declarations.Count - declarations
            .DistinctBy(x => x.Id, StringComparer.Ordinal)
            .Count();

        if (duplicateIds.Count > 0)
        {
            logger.LogInformation(
                "[CorrelationId: {CorrelationId}] PAYE scheme {EmpRef} has duplicate HMRC levy declaration id(s): {DuplicateIds}",
                input.CorrelationId,
                input.EmpRef,
                string.Join(", ", duplicateIds));
        }

        return declarations
            .DistinctBy(x => x.Id, StringComparer.Ordinal)
            .ToList();
    }

    private static List<NormalizedLevyDeclaration> FilterExistingDeclarations(
        NormalizeLevyDeclarationsInput input,
        List<NormalizedLevyDeclaration> declarations,
        out int existingDeclarationCount)
    {
        var existingSubmissionIds = new HashSet<string>(
            input.ExistingSubmissionIds ?? [],
            StringComparer.Ordinal);

        var filteredDeclarations = declarations
            .Where(x => !existingSubmissionIds.Contains(x.Id))
            .ToList();

        existingDeclarationCount = declarations.Count - filteredDeclarations.Count;

        return filteredDeclarations;
    }

    private static List<NormalizedLevyDeclaration> FilterPreLevyDeclarations(
        NormalizeLevyDeclarationsInput input,
        List<NormalizedLevyDeclaration> declarations,
        out int preLevyDeclarationCount)
    {
        var filteredDeclarations = declarations
            .Where(x => !DoesSubmissionPreDateLevy(x.PayrollYear))
            .ToList();

        preLevyDeclarationCount = declarations.Count - filteredDeclarations.Count;

        return filteredDeclarations;
    }

    private static List<NormalizedLevyDeclaration> FilterFutureDeclarations(
        NormalizeLevyDeclarationsInput input,
        List<NormalizedLevyDeclaration> declarations,
        out int futureDeclarationCount)
    {
        var filteredDeclarations = declarations
            .Where(x => !IsSubmissionForFuturePeriod(x.PayrollYear, x.PayrollMonth, input.ProcessingDate))
            .ToList();

        futureDeclarationCount = declarations.Count - filteredDeclarations.Count;

        return filteredDeclarations;
    }

    private static void ProcessNoPaymentForPeriodDeclarations(List<NormalizedLevyDeclaration> declarations)
    {
        foreach (var declaration in declarations.Where(x => x.NoPaymentForPeriod))
        {
            declaration.LevyDueYtd = null;
        }
    }

    private static void SetEndOfYearAdjustmentProperties(List<NormalizedLevyDeclaration> declarations)
    {
        foreach (var declaration in declarations)
        {
            declaration.EndOfYearAdjustment = IsEndOfYearAdjustment(declaration.PayrollYear, declaration.PayrollMonth, declaration.SubmissionDate);
        }
    }

    private static void ProcessEndOfYearAdjustmentDeclarations(
        NormalizeLevyDeclarationsInput input,
        List<NormalizedLevyDeclaration> declarations)
    {
        foreach (var declaration in declarations.Where(x => x.EndOfYearAdjustment))
        {
            UpdateEndOfYearAdjustment(input, declaration, declarations);
        }
    }

    private static void UpdateEndOfYearAdjustment(
        NormalizeLevyDeclarationsInput input,
        NormalizedLevyDeclaration yearEndAdjustment,
        List<NormalizedLevyDeclaration> declarations)
    {
        if (yearEndAdjustment.LevyDueYtd == null && !yearEndAdjustment.NoPaymentForPeriod)
        {
            throw new ArgumentNullException(nameof(yearEndAdjustment));
        }

        yearEndAdjustment.EndOfYearAdjustment = true;

        if (yearEndAdjustment.NoPaymentForPeriod)
        {
            return;
        }

        var period12Declaration = GetDeclarationEffectiveForPeriod12(
            input.ExistingPeriod12Declarations ?? [],
            yearEndAdjustment.PayrollYear,
            yearEndAdjustment.SubmissionDate,
            declarations);

        yearEndAdjustment.EndOfYearAdjustmentAmount = period12Declaration?.LevyDueYtd != null
            ? period12Declaration.LevyDueYtd.Value - (yearEndAdjustment.LevyDueYtd ?? 0)
            : -(yearEndAdjustment.LevyDueYtd ?? 0);
    }

    private static NormalizedLevyDeclaration? GetDeclarationEffectiveForPeriod12(
        List<NormalizedLevyDeclaration> existingPeriod12Declarations,
        string payrollYear,
        DateTime yearEndAdjustmentCutOff,
        List<NormalizedLevyDeclaration> hmrcDeclarations)
    {
        return GetEffectivePeriod12SubmissionFromLatestHmrcFeed(hmrcDeclarations, payrollYear, yearEndAdjustmentCutOff)
               ?? GetEffectivePeriod12SubmissionFromExistingDeclarations(existingPeriod12Declarations, payrollYear, yearEndAdjustmentCutOff);
    }

    private static NormalizedLevyDeclaration? GetEffectivePeriod12SubmissionFromLatestHmrcFeed(
        List<NormalizedLevyDeclaration> declarations,
        string payrollYear,
        DateTime yearEndAdjustmentCutOff)
    {
        return declarations
                   .Where(x => x.EndOfYearAdjustment)
                   .OrderByDescending(x => x.SubmissionDate)
                   .FirstOrDefault(x => IsAnEarlierYearEndAdjustment(x, payrollYear, yearEndAdjustmentCutOff))
               ?? declarations
                   .Where(x => !x.EndOfYearAdjustment)
                   .OrderByDescending(x => x.SubmissionDate)
                   .FirstOrDefault(x => IsPossibleEffectivePeriod12Declaration(x, payrollYear));
    }

    private static NormalizedLevyDeclaration? GetEffectivePeriod12SubmissionFromExistingDeclarations(
        List<NormalizedLevyDeclaration> declarations,
        string payrollYear,
        DateTime yearEndAdjustmentCutOff)
    {
        return declarations
            .OrderByDescending(x => x.SubmissionDate)
            .FirstOrDefault(x =>
                x.PayrollYear == payrollYear &&
                x.SubmissionDate < yearEndAdjustmentCutOff &&
                x.LevyDueYtd.HasValue);
    }

    private static bool DoesSubmissionPreDateLevy(string payrollYear)
    {
        if (string.IsNullOrWhiteSpace(payrollYear))
        {
            return false;
        }

        var yearSplit = payrollYear.Split('-');

        return int.TryParse(yearSplit[0], out var yearStart) && yearStart <= 16;
    }

    private static bool IsSubmissionForFuturePeriod(string payrollYear, short? payrollMonth, DateTime processingDate)
    {
        return payrollMonth.HasValue && GetDateFromPayrollYearMonth(payrollYear, payrollMonth.Value).AddMonths(1) > processingDate;
    }

    private static bool IsEndOfYearAdjustment(string payrollYear, short? payrollMonth, DateTime submissionDate)
    {
        if (payrollMonth != 12)
        {
            return false;
        }

        var (_, endDate) = GetPayrollYearRange(payrollYear);

        return submissionDate >= endDate.AddDays(21);
    }

    private static bool IsPossibleEffectivePeriod12Declaration(NormalizedLevyDeclaration declaration, string payrollYear)
    {
        return declaration.PayrollYear == payrollYear &&
               declaration.PayrollMonth.HasValue &&
               !declaration.EndOfYearAdjustment &&
               IsDateOntimeForPayrollPeriod(declaration.PayrollYear, declaration.PayrollMonth.Value, declaration.SubmissionDate);
    }

    private static bool IsAnEarlierYearEndAdjustment(
        NormalizedLevyDeclaration declaration,
        string payrollYear,
        DateTime yearEndAdjustmentCutOff)
    {
        return declaration.PayrollYear == payrollYear &&
               declaration.PayrollMonth.HasValue &&
               declaration.EndOfYearAdjustment &&
               declaration.SubmissionDate < yearEndAdjustmentCutOff;
    }

    private static bool IsDateOntimeForPayrollPeriod(string payrollYear, int payrollMonth, DateTime dateTime)
    {
        var payrollPeriodStart = GetDateFromPayrollYearMonth(payrollYear, payrollMonth);
        var payrollPeriodEnd = payrollPeriodStart.AddMonths(1).AddMilliseconds(-1);

        return dateTime <= payrollPeriodEnd;
    }

    private static (DateTime StartDate, DateTime EndDate) GetPayrollYearRange(string payrollYear)
    {
        var yearSplit = payrollYear.Split('-');

        var startDate = new DateTime(Convert.ToInt32("20" + yearSplit[0]), 4, 1);
        var endDate = new DateTime(Convert.ToInt32("20" + yearSplit[1]), 3, 31, 23, 59, 59);

        return (startDate, endDate);
    }

    private static DateTime GetDateFromPayrollYearMonth(string payrollYear, int payrollMonth)
    {
        var yearSplit = payrollYear.Split('-');
        var yearToUse = 2000;
        int monthToUse;

        if (payrollMonth is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(payrollMonth), payrollMonth, "Payroll month must be between 1 and 12.");
        }

        if (payrollMonth >= 10)
        {
            yearToUse += Convert.ToInt32(yearSplit[1]);
            monthToUse = payrollMonth - 9;
        }
        else
        {
            yearToUse += Convert.ToInt32(yearSplit[0]);
            monthToUse = payrollMonth + 3;
        }

        return new DateTime(yearToUse, monthToUse, 20);
    }
}
