using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;
using System.Text.Json.Serialization;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

public interface IGetApiRequest : IApiRequest
{
    [JsonIgnore]
    string GetUrl { get; }
}