using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace SFA.DAS.Employer.Finance.Jobs.Functions.Extensions;

public static class DataProtectionExtensions
{
    public static IServiceCollection AddDasDataProtection(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration["RedisConnectionString"];
        var dataProtectionKeysDatabase = configuration["DataProtectionKeysDatabase"];

        if (string.IsNullOrEmpty(redisConnectionString) || string.IsNullOrEmpty(dataProtectionKeysDatabase))
        {
            return services;
        }
        
        var redis = ConnectionMultiplexer.Connect($"{redisConnectionString},{dataProtectionKeysDatabase}");
        
        services.AddDataProtection()
            .SetApplicationName("das-employer-finance-jobs")
            .PersistKeysToStackExchangeRedis(redis, "DataProtection-Keys");

        return services;
    }
}
