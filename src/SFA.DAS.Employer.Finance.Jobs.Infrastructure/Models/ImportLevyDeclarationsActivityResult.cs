using HMRC.ESFA.Levy.Api.Types;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public record ImportLevyDeclarationsActivityResult(
    string EmpRef,
    DateTime? FromDate,
    int DeclarationsCount,
    LevyDeclarations LevyDeclarations);
