using Microsoft.Extensions.Logging;
using SFA.DAS.Api.Common.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Abstractions;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces.Services;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;

public class PaymentApiClient : BaseApiClient, IPaymentApiClient
{
    public PaymentApiClient(IHttpClientFactory httpClientFactory,
                            IAzureClientCredentialHelper credentialHelper,
                            PaymentApiConfiguration configuration,
                            ILogger<PaymentApiClient> logger)

        : base(httpClientFactory.CreateClient(), credentialHelper, logger, configuration)
    {
    }
}
