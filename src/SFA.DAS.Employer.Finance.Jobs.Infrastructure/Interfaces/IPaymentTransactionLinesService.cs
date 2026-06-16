using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces
{
    public interface IPaymentTransactionLinesService
    {
        Task<CreatePaymentTransactionLinesResult> CreatePaymentTransactionLines(CreatePaymentTransactionLinesInput input);
    }
}
