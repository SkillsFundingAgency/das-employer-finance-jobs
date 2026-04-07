using Microsoft.Extensions.Logging;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Configuration;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.SharedApi.Interfaces;
using System.Text.Json;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;

public class AccountService(IFinanceApiClient<FinanceApiConfiguration> financeApiClient, ILogger<IAccountService> logger) : IAccountService
{
    public async Task<List<Accounts>> GetAccountsAsync(GetAccountsRequest request)
    {
        try
        {
            logger.LogInformation("Calling Finance API to get accounts, page {Page}, pageSize {PageSize}", request.Page, request.PageSize);

            var response = await financeApiClient.Get<FinanceApiGetAccountsResponse>(request);

            var accounts = response?.Accounts ?? [];
            logger.LogInformation("Finance API returned {Count} accounts for page {Page}", accounts.Count, request.Page);

            return accounts;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting accounts from Finance API for page {Page}: {ErrorMessage}", request.Page, ex.Message);
            throw;
        }
    }

    public async Task<List<PayeScheme>> GetPayeSchemesAsync(GetAccountPayeSchemesRequest request)
    {
        try
        {
            logger.LogInformation(
                "Calling Finance API to get PAYE schemes for account {AccountId} from source {Source}",
                request.AccountId,
                request.Source);

            var response = await financeApiClient.Get<JsonElement>(request);
            var payeSchemes = ParsePayeSchemes(response);

            logger.LogInformation(
                "Finance API returned {Count} PAYE schemes for account {AccountId}",
                payeSchemes.Count,
                request.AccountId);

            return payeSchemes;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Error getting PAYE schemes from Finance API for account {AccountId}: {ErrorMessage}",
                request.AccountId,
                ex.Message);
            throw new InvalidOperationException(
                $"Failed to get PAYE schemes for account {request.AccountId}.",
                ex);
        }
    }

    private static List<PayeScheme> ParsePayeSchemes(JsonElement response)
    {
        if (response.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return [];
        }

        var payeSchemesElement = response;

        if (response.ValueKind == JsonValueKind.Object)
        {
            if (TryGetProperty(response, "payeSchemes", out var wrappedPayeSchemes) ||
                TryGetProperty(response, "schemes", out wrappedPayeSchemes))
            {
                payeSchemesElement = wrappedPayeSchemes;
            }
            else
            {
                return [];
            }
        }

        if (payeSchemesElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var payeSchemes = new List<PayeScheme>();

        foreach (var item in payeSchemesElement.EnumerateArray())
        {
            var payeScheme = ParsePayeScheme(item);
            if (!string.IsNullOrWhiteSpace(payeScheme?.Reference))
            {
                payeSchemes.Add(payeScheme);
            }
        }

        return payeSchemes;
    }

    private static PayeScheme? ParsePayeScheme(JsonElement item)
    {
        if (item.ValueKind == JsonValueKind.String)
        {
            var stringReference = item.GetString();
            return string.IsNullOrWhiteSpace(stringReference)
                ? null
                : new PayeScheme { Reference = stringReference };
        }

        if (item.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var reference = GetStringProperty(item, "empRef")
                        ?? GetStringProperty(item, "ref")
                        ?? GetStringProperty(item, "payeRef")
                        ?? GetStringProperty(item, "schemeReference");

        if (string.IsNullOrWhiteSpace(reference))
        {
            return null;
        }

        return new PayeScheme
        {
            Reference = reference,
            Name = GetStringProperty(item, "name") ?? string.Empty
        };
    }

    private static bool TryGetProperty(JsonElement element, string propertyName, out JsonElement value)
    {
        var matchingProperty = element.EnumerateObject()
            .Where(property => string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            .Select(property => (JsonElement?)property.Value)
            .FirstOrDefault();

        if (matchingProperty.HasValue)
        {
            value = matchingProperty.Value;
            return true;
        }

        value = default;
        return false;
    }

    private static string? GetStringProperty(JsonElement element, string propertyName)
    {
        return TryGetProperty(element, propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }
}
