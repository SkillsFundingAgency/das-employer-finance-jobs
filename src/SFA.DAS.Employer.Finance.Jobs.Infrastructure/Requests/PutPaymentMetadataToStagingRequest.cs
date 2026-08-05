using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;

public class PutPaymentMetadataToStagingRequest(Guid paymentId, object data) : IApiRequest
{
    public string GetUrl => $"api/payments/{paymentId}/metadata/staging";
    public object Data { get; set; } = data;
}
