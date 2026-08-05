using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Services;

public interface ILevyDeclarationNormalizer
{
    NormalizeLevyDeclarationsResult Normalize(NormalizeLevyDeclarationsInput input);
}
