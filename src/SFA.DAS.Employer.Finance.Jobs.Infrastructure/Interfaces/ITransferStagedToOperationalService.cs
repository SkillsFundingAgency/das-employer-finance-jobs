using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;

public interface ITransferStagedToOperationalService
{
    Task<TransferStagedToOperationalResult> Process(TransferStagedToOperationalInput input);
}
