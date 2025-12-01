using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Abstractions;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces.Services;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;
public class FinanceApiClient : BaseApiClient, IFinanceApiClient
{
    public FinanceApiClient(IHttpClientFactory httpClientFactory,
                            IAzureClientCredentialHelper credentialHelper,
                            FinanceApiConfiguration configuration,
                            ILogger<FinanceApiClient> logger)

        : base(httpClientFactory.CreateClient(), credentialHelper, logger, configuration)
    {
    }
}