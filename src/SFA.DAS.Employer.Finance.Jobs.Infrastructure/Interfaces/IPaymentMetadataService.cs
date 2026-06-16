using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Provider.Events.Api.Types;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

public interface IPaymentMetadataService
{
    Task<CreatePaymentMetadataResult> CreatePaymentMetadata(CreatePaymentMetadataInput input, CancellationToken cancellationToken);
    Task<PaymentMetadataStaging> BuildPaymentMetadata(long accountId, Payment payment, Guid correlationId);
}
