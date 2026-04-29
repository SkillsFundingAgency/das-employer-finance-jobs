using HMRC.ESFA.Levy.Api.Client;
using HMRC.ESFA.Levy.Api.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SFA.DAS.ActiveDirectory;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Exceptions;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces.HMRC;
using SFA.DAS.TokenService.Api.Client;
using System.Net;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services.HMRC;

public class HmrcService(
        IHmrcConfiguration configuration,
        IApprenticeshipLevyApiClient apprenticeshipLevyApiClient,
        ITokenServiceApiClient tokenServiceApiClient,
        IAzureAdAuthenticationService azureAdAuthenticationService,
        IOptions<LevyImportResilienceOptions> resilienceOptions,
        IHmrcRateLimiter hmrcRateLimiter,
        ILogger<HmrcService> logger) : IHmrcService
{
    private readonly IHmrcConfiguration _configuration = configuration;
    private readonly IApprenticeshipLevyApiClient _apprenticeshipLevyApiClient = apprenticeshipLevyApiClient;
    private readonly IAzureAdAuthenticationService _azureAdAuthenticationService = azureAdAuthenticationService;
    private readonly ITokenServiceApiClient _tokenServiceApiClient = tokenServiceApiClient;
    private readonly LevyImportResilienceOptions _resilienceOptions = ValidateResilienceOptions(resilienceOptions.Value);
    private readonly IHmrcRateLimiter _hmrcRateLimiter = hmrcRateLimiter;
    private readonly ILogger<HmrcService> _logger = logger;

    public async Task<LevyDeclarations> GetLevyDeclarations(string empRef, DateTime? fromDate, string correlationId, CancellationToken cancellationToken)
    {

        var declarations = await GetLevyDeclarationsWithResilienceAsync(empRef, fromDate, correlationId, cancellationToken);
        declarations ??= new LevyDeclarations { Declarations = [] };

        _logger.LogInformation(
            "[CorrelationId: {CorrelationId}] Imported {DeclarationsCount} levy declarations for employee reference {EmployeeReference} from {DateFrom}",
            correlationId,
            declarations.Declarations?.Count ?? 0,
            empRef,
            fromDate);

        return declarations;
    }

    private static LevyImportResilienceOptions ValidateResilienceOptions(LevyImportResilienceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.MaxRetries < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.MaxRetries), options.MaxRetries, "Maximum retries must be zero or greater.");
        }

        if (options.BaseDelayMilliseconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.BaseDelayMilliseconds), options.BaseDelayMilliseconds, "Base delay must be zero or greater.");
        }

        if (options.JitterMilliseconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.JitterMilliseconds), options.JitterMilliseconds, "Jitter must be zero or greater.");
        }

        if (options.MaxRequestsPerWindow <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.MaxRequestsPerWindow), options.MaxRequestsPerWindow, "Maximum requests per window must be greater than zero.");
        }

        if (options.WindowSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options.WindowSeconds), options.WindowSeconds, "Rate-limit window must be greater than zero.");
        }

        return options;
    }

    private async Task<LevyDeclarations> GetLevyDeclarationsWithResilienceAsync(
        string empRef,
        DateTime? fromDate,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var accessToken = await GetOgdAccessToken();

        var earliestDate = new DateTime(2017, 04, 01);
        if (!fromDate.HasValue || fromDate.Value < earliestDate) fromDate = earliestDate;

        var attempts = _resilienceOptions.MaxRetries + 1;
        Exception? lastException = null;
        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                var waitDuration = await _hmrcRateLimiter.WaitForAvailabilityAsync(cancellationToken);
                if (waitDuration > TimeSpan.Zero)
                {
                    _logger.LogInformation(
                        "[CorrelationId: {CorrelationId}] HMRC levy request delayed due to throttling for {EmployeeReference}. Wait duration: {WaitDurationMs}ms",
                        correlationId,
                        empRef,
                        waitDuration.TotalMilliseconds);
                }

                return await _apprenticeshipLevyApiClient.GetEmployerLevyDeclarations(accessToken, empRef, fromDate);
            }
            catch (Exception ex) when (IsTransient(ex))
            {
                lastException = ex;
                if (attempt == attempts)
                {
                    break;
                }

                if (ex is HmrcApiException { StatusCode: HttpStatusCode.TooManyRequests } tooManyRequestsException)
                {
                    _logger.LogWarning(
                        tooManyRequestsException,
                        "[CorrelationId: {CorrelationId}] HMRC returned 429 for {EmployeeReference} on attempt {Attempt}",
                        correlationId,
                        empRef,
                        attempt);
                }

                var delay = GetRetryDelay(attempt);
                _logger.LogWarning(ex,
                    "[CorrelationId: {CorrelationId}] Retrying HMRC levy request for {EmployeeReference} after {DelayMs}ms (attempt {Attempt} of {TotalAttempts})",
                    correlationId,
                    empRef,
                    delay.TotalMilliseconds,
                    attempt,
                    attempts);

                await Task.Delay(delay, cancellationToken);
            }
        }

        throw lastException ?? new InvalidOperationException("HMRC levy request failed without exception details.");
    }

    private bool IsTransient(Exception ex)
    {
        return ex switch
        {
            HmrcApiException { StatusCode: HttpStatusCode.TooManyRequests or HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout } => true,
            HttpRequestException => true,
            TaskCanceledException => true,
            _ => false
        };
    }

    private TimeSpan GetRetryDelay(int attempt)
    {
        var exponentialDelay = _resilienceOptions.BaseDelayMilliseconds * Math.Pow(2, attempt - 1);
        var jitter = _resilienceOptions.JitterMilliseconds <= 0
            ? 0
            : Random.Shared.Next(0, _resilienceOptions.JitterMilliseconds + 1);

        return TimeSpan.FromMilliseconds(exponentialDelay + jitter);
    }
    private async Task<string> GetOgdAccessToken()
    {
        if (_configuration.UseHiDataFeed)
        {
            return await _azureAdAuthenticationService.GetAuthenticationResult(
                _configuration.ClientId,
                _configuration.AzureAppKey,
                _configuration.AzureResourceId,
                _configuration.AzureTenant);
        }
        else
        {
            var accessToken = await _tokenServiceApiClient.GetPrivilegedAccessTokenAsync();
            return accessToken.AccessCode;
        }
    }
}
