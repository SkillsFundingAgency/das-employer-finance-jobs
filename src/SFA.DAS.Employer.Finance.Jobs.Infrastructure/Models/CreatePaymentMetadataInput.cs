using SFA.DAS.Provider.Events.Api.Types;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

public class CreatePaymentMetadataInput
{
    public long AccountId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public List<Payment> PaymentDetails { get; set; } = [];
}
