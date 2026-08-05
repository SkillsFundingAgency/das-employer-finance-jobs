using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;

public class PersistEnglishFractionsRequest : IApiRequest
{
    public string GetUrl => "api/english-fractions";
    public object? Data { get; set; }
}

public class PersistEnglishFractionsRequestData
{
    public string EmpRef { get; set; } = string.Empty;
    public bool UpdateRequired { get; set; }
    public DateTime DateCalculated { get; set; }
    public List<PersistEnglishFractionItem> Fractions { get; set; } = [];
}

public class PersistEnglishFractionItem
{
    public DateTime DateCalculated { get; set; }
    public decimal Amount { get; set; }
}
