using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Models;

public record AccountPayeSchemes(long AccountId, List<PayeScheme> PayeSchemes);
