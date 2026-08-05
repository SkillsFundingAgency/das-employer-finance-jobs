using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Provider.Events.Api.Types;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

public interface IRefreshPaymentDataService
{
    Task<RefreshPaymentDataResult> PostPaymentsToStaging(List<PaymentStaging> filteredPayments, string correlationId);
    List<PaymentStaging> FilterPayments(List<Payment> externalPayments, List<string> existingPaymentIds, long accountId, string correlationId);
}
