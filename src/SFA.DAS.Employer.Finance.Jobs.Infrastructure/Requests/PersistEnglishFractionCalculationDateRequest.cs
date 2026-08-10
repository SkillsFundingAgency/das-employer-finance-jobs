using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;

public class PersistEnglishFractionCalculationDateRequest : IApiRequest
{
    public string GetUrl => "api/english-fraction-calculation-date";
    public object Data { get; set; } = null!;
}

public class PersistEnglishFractionCalculationDateRequestData
{
    public DateTime DateCalculated { get; set; }
}
