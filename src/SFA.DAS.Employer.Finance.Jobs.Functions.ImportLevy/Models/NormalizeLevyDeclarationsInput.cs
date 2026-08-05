using HMRC.ESFA.Levy.Api.Types;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Models;

public class NormalizeLevyDeclarationsInput
{
    public string CorrelationId { get; set; } = string.Empty;
    public long AccountId { get; set; }
    public string EmpRef { get; set; } = string.Empty;

    public List<Declaration> HmrcDeclarations { get; set; } = [];

    // Placeholder for the persistence side: these should come from Finance before this activity is called.
    public List<string> ExistingSubmissionIds { get; set; } = [];

    // Placeholder for legacy year-end adjustment enrichment when the effective period 12 declaration is not in the HMRC fetch result.
    public List<NormalizedLevyDeclaration> ExistingPeriod12Declarations { get; set; } = [];

    public DateTime ProcessingDate { get; set; }
}
