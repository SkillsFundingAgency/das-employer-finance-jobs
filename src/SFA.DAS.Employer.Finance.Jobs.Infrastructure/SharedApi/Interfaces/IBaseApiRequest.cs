using System.Text.Json.Serialization;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces
{
    public interface IBaseApiRequest
    {
        [JsonIgnore]
        string Version => "1.0";
    }
}