using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;

public class ImportAccountPaymentsRequest : IApiRequest
{
    public string GetUrl => "api/imports/account-payments";

    public object Data { get; set; }

    public AccountPaymentsImportRequest Payload
    {
        set => Data = value;
    }
}
