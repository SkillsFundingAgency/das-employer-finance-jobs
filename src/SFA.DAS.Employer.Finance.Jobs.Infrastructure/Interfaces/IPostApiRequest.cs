using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;
using System.Text.Json.Serialization;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces
{
    public interface IPostApiRequest : IPostApiRequest<object>
    {
    }

    public interface IPostApiRequest<TData> : IBaseApiRequest
    {
        [JsonIgnore]
        string PostUrl { get; }
        TData Data { get; set; }
    }
}
