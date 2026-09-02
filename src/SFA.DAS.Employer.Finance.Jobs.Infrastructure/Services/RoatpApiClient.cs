using Azure.Identity;
using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Requests;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;

public class RoatpApiClient(
    IInternalApiClient<RoatpApiConfiguration> apiClient,
    ILogger<RoatpApiClient> logger) : IRoatpApiClient
{
    public async Task<ProviderDetails?> GetProvider(long ukprn)
    {
        try
        {
            var response = await apiClient.Get<RoatpProviderApiResponse>(new GetRoatpProviderRequest(ukprn));
            if (response == null)
            {
                return null;
            }

            return new ProviderDetails
            {
                Ukprn = response.Ukprn,
                Name = response.Name,
                IsHistoricProviderName = false
            };
        }
        catch (Exception ex) when (IsAuthenticationFailure(ex))
        {
            logger.LogError(
                ex,
                "RoATP authentication failed for Ukprn {Ukprn}. Provider details cannot be treated as missing.",
                ukprn);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unable to get provider details for Ukprn {Ukprn} from RoATP API.", ukprn);
            return null;
        }
    }

    private static bool IsAuthenticationFailure(Exception exception)
    {
        return exception is AuthenticationFailedException
               || exception.GetBaseException() is AuthenticationFailedException;
    }
}
