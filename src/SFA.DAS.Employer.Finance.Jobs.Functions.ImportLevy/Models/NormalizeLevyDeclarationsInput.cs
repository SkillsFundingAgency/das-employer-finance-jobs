<<<<<<< HEAD
using HMRC.ESFA.Levy.Api.Types;

=======
>>>>>>> 8ec3ca5367ae1ad2a7507a98b45f20b5f7ab141c
namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Models;

public class NormalizeLevyDeclarationsInput
{
    public string CorrelationId { get; set; } = string.Empty;
    public long AccountId { get; set; }
    public string EmpRef { get; set; } = string.Empty;

<<<<<<< HEAD
    public List<Declaration> HmrcDeclarations { get; set; } = [];
=======
    // APPMAN-2548 placeholder: populated by the HMRC levy declarations fetch activity once that dependency lands.
    public List<HmrcLevyDeclaration> HmrcDeclarations { get; set; } = [];
>>>>>>> 8ec3ca5367ae1ad2a7507a98b45f20b5f7ab141c

    // Placeholder for the persistence side: these should come from Finance before this activity is called.
    public List<string> ExistingSubmissionIds { get; set; } = [];

    // Placeholder for legacy year-end adjustment enrichment when the effective period 12 declaration is not in the HMRC fetch result.
    public List<NormalizedLevyDeclaration> ExistingPeriod12Declarations { get; set; } = [];

    public DateTime ProcessingDate { get; set; }
}
