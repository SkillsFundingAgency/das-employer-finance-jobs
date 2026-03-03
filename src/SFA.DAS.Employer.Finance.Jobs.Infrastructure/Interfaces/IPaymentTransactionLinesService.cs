using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models.PaymentTransactions;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces
{
    public interface IPaymentTransactionLinesService
    {
        Task<CreatePaymentTransactionLinesResult> CreatePaymentTransactionLines(CreatePaymentTransactionLinesInput input);
    }
}
