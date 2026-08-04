namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public class ImportLevyResult
{
    public bool Success { get; set; }
    public int TotalAccountsCount { get; set; }
    public int TotalPayeSchemesCount { get; set; }
    public int AccountsWithoutPayeSchemesCount { get; set; }
    public List<long> AccountIds { get; set; } = [];
    public List<PayeScheme> PayeSchemes { get; set; } = [];
    public List<ImportLevyDeclarationsActivityResult> LevyDeclarationsActivityResults { get; set; } = [];
    public string ErrorMessage { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
}
