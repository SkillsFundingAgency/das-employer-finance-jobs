using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SFA.DAS.Api.Common.Infrastructure;
using SFA.DAS.Api.Common.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Services;
using System.Diagnostics.CodeAnalysis;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Extensions;
[ExcludeFromCodeCoverage]
public static class ServiceRegistrationExtensions
{
    public static void AddServiceRegistration(this IServiceCollection services, IConfiguration configuration)
    {        

        services.AddHttpClient();
     
        services.AddSingleton<IAzureClientCredentialHelper, AzureClientCredentialHelper>();

        services.AddTransient(typeof(IInternalApiClient<>), typeof(InternalApiClient<>));

        services.AddTransient<IProviderPaymentApiClient<ProviderEventsApiConfiguration>, ProviderPaymentApiClient>();

        services.AddTransient<IFinanceApiClient<FinanceApiConfiguration>, FinanceApiClient>();

        services.AddScoped<IPeriodEndService, PeriodEndService>();
    }
}