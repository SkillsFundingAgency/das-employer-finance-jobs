using System.Text.Json.Serialization;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

public interface IApiRequest : IBaseApiRequest
{
    [JsonIgnore]
    string GetUrl { get; }

    [JsonIgnore]
    object? Data { get; }

}