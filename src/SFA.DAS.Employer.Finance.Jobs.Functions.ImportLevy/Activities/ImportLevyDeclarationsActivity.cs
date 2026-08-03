using HMRC.ESFA.Levy.Api.Types;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces.HMRC;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Activities;

public class ImportLevyDeclarationsActivity(
    IHmrcService hmrcService,
    ILogger<ImportLevyDeclarationsActivity> logger)
{
    private readonly IHmrcService _hmrcService = hmrcService;
    private readonly ILogger<ImportLevyDeclarationsActivity> _logger = logger;

    [Function(nameof(ImportLevyDeclarationsActivity))]
    public async Task<ImportLevyDeclarationsActivityResult> Run([ActivityTrigger] ImportLevyActivityRequest request, FunctionContext context)
    {
        _logger.LogInformation(
            "[CorrelationId: {CorrelationId}] Starting levy import for payee reference {EmployeeReference}, from date {FromDate}",
            request.CorrelationId,
            request.EmpRef,
            request.FromDate);

        var levyDeclarations = await _hmrcService.GetLevyDeclarations(request.EmpRef, request.FromDate, request.CorrelationId, context.CancellationToken);

        _logger.LogInformation(
            "[CorrelationId: {CorrelationId}] Imported levy declarations {Count} for payee reference {EmployeeReference}, from date {FromDate}",
            request.CorrelationId,
            levyDeclarations == null ? 0 : levyDeclarations.Declarations.Count,
            request.EmpRef,
            request.FromDate);

        return new ImportLevyDeclarationsActivityResult(
            request.EmpRef,
            request.FromDate,
            levyDeclarations == null ? 0 : levyDeclarations.Declarations.Count,
            levyDeclarations == null ? new LevyDeclarations() : levyDeclarations);
    }
}
