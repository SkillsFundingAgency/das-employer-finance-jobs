using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests.FundingProjection;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.Models;

public sealed record PostJobActivityInput(Guid JobId, PostJobRequestData Payload);
