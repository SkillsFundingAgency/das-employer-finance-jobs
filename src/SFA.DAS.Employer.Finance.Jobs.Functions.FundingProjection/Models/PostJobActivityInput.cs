using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests.FundingProjection;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.FundingProjection.Models;

public sealed record PostJobActivityInput(Guid JobId, PostJobRequestData Payload);
