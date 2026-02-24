using Azure;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses
{
    public abstract class BaseResponse<T> : Response
    {
        public T Result { get; set; }
    }
}
