using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;
using System.Text.Json.Serialization;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

public interface IApiRequest : IBaseApiRequest
{
    [JsonIgnore]
    string GetUrl { get; }

    object Data { get; }

}

