using SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.ImportLevy.Requests;

public class PersistLevyDeclarationsRequest(PersistLevyDeclarationRequestData data) : IApiRequest
{
    public string GetUrl => "api/levy-declarations";

    public object Data { get; } = data;
}
