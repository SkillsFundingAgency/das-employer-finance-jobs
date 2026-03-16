using System.Net;
using System.Text.Json;
using WireMock;
using WireMock.Logging;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Settings;
using WireMock.Types;
using WireMock.Util;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Responses;

namespace SFA.DAS.Employer.Finance.Jobs.FakeServers;

public static class Program
{
    private const int DefaultFinancePort = 5061;
    private const int DefaultProviderEventsPort = 5062;

    public static void Main(string[] args)
    {
        var financePort = GetPort(args, "--finance-port", DefaultFinancePort);
        var providerEventsPort = GetPort(args, "--provider-events-port", DefaultProviderEventsPort);
        var repeatable = !args.Contains("--no-repeatable", StringComparer.OrdinalIgnoreCase);

        var financeApi = StartFinanceApi(financePort, repeatable);
        var providerEventsApi = StartProviderEventsApi(providerEventsPort);

        Console.WriteLine($"Finance Fake API running ({financeApi.Urls[0]})");
        Console.WriteLine($"Provider Events Fake API running ({providerEventsApi.Urls[0]})");
        Console.WriteLine("Press any key to stop the APIs server");
        Console.ReadKey();

        financeApi.Stop();
        providerEventsApi.Stop();
    }

    private static WireMockServer StartFinanceApi(int port, bool repeatable)
    {
        var server = WireMockServer.Start(new WireMockServerSettings
        {
            Port = port,
            UseSSL = false,
            StartAdminInterface = true,
            Logger = new WireMockConsoleLogger()
        });

        var financePeriodEnds = BuildFinancePeriodEnds();
        var accounts = BuildAccounts();
        var nextPeriodEndId = financePeriodEnds.Max(pe => pe.Id) + 1;

        server
            .Given(Request.Create().WithPath("/api/period-ends").UsingGet())
            .RespondWith(Response.Create().WithCallback(_ => BuildPeriodEndsResponse(repeatable ? [] : financePeriodEnds)));

        server
            .Given(Request.Create().WithPath("/api/period-ends").UsingPost())
            .RespondWith(Response.Create().WithCallback(request =>
            {
                var body = request.Body;
                var periodEnd = string.IsNullOrWhiteSpace(body)
                    ? null
                    : JsonSerializer.Deserialize<PeriodEnd>(body, JsonSerializerOptions);

                if (periodEnd == null)
                {
                    return BuildErrorResponse(HttpStatusCode.BadRequest, "Missing period end payload.");
                }

                if (periodEnd.Id == 0)
                {
                    periodEnd.Id = nextPeriodEndId++;
                }

                if (!financePeriodEnds.Any(pe => string.Equals(pe.PeriodEndId, periodEnd.PeriodEndId, StringComparison.OrdinalIgnoreCase)))
                {
                    financePeriodEnds.Add(periodEnd);
                }

                return BuildJsonResponse(periodEnd);
            }));

        server
            .Given(Request.Create().WithPath("/api/accounts").UsingGet())
            .RespondWith(Response.Create().WithCallback(request => BuildAccountsResponse(request, accounts)));

        server
            .Given(Request.Create().WithPath("/api/imports/account-payments").UsingPost())
            .RespondWith(Response.Create().WithCallback(_ =>
            {
                var response = new FinanceApiAccountPaymentsImportResponse
                {
                    ImportId = Guid.NewGuid(),
                    Status = "Accepted",
                    AcceptedAt = DateTime.UtcNow
                };
                return BuildJsonResponse(response);
            }));

        return server;
    }

    private static WireMockServer StartProviderEventsApi(int port)
    {
        var server = WireMockServer.Start(new WireMockServerSettings
        {
            Port = port,
            UseSSL = false,
            StartAdminInterface = true,
            Logger = new WireMockConsoleLogger()
        });

        var paymentPeriodEnds = BuildPaymentPeriodEnds();

        server
            .Given(Request.Create().WithPath("/api/periodends").UsingGet())
            .RespondWith(Response.Create().WithCallback(_ => BuildJsonResponse(paymentPeriodEnds)));

        return server;
    }

    private static ResponseMessage BuildAccountsResponse(IRequestMessage request, List<Accounts> accounts)
    {
        var query = request.Query;
        var pageNumber = GetQueryInt(query, "pageNumber", 1);
        var pageSize = GetQueryInt(query, "pageSize", 100);

        var paged = accounts
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var response = new FinanceApiGetAccountsResponse
        {
            Accounts = paged
        };

        return BuildJsonResponse(response);
    }

    private static ResponseMessage BuildPeriodEndsResponse(List<PeriodEnd> periodEnds)
    {
        return BuildJsonResponse(periodEnds);
    }

    private static ResponseMessage BuildJsonResponse<T>(T payload)
    {
        var body = JsonSerializer.Serialize(payload, JsonSerializerOptions);

        return new ResponseMessage
        {
            StatusCode = (int)HttpStatusCode.OK,
            BodyData = new BodyData
            {
                DetectedBodyType = BodyType.String,
                BodyAsString = body
            },
            Headers = new Dictionary<string, WireMockList<string>>
            {
                { "Content-Type", new WireMockList<string>("application/json") }
            }
        };
    }

    private static ResponseMessage BuildErrorResponse(HttpStatusCode statusCode, string message)
    {
        var body = JsonSerializer.Serialize(new { error = message }, JsonSerializerOptions);

        return new ResponseMessage
        {
            StatusCode = (int)statusCode,
            BodyData = new BodyData
            {
                DetectedBodyType = BodyType.String,
                BodyAsString = body
            },
            Headers = new Dictionary<string, WireMockList<string>>
            {
                { "Content-Type", new WireMockList<string>("application/json") }
            }
        };
    }

    private static int GetPort(string[] args, string key, int defaultValue)
    {
        var index = Array.FindIndex(args, arg => string.Equals(arg, key, StringComparison.OrdinalIgnoreCase));
        if (index < 0 || index == args.Length - 1)
        {
            return defaultValue;
        }

        return int.TryParse(args[index + 1], out var port) ? port : defaultValue;
    }

    private static int GetQueryInt(IDictionary<string, WireMockList<string>>? query, string key, int defaultValue)
    {
        if (query == null)
        {
            return defaultValue;
        }

        if (!query.TryGetValue(key, out var values))
        {
            return defaultValue;
        }

        return int.TryParse(values.FirstOrDefault(), out var result) ? result : defaultValue;
    }

    private static List<Accounts> BuildAccounts()
    {
        return Enumerable.Range(1, 25)
            .Select(i => new Accounts
            {
                Id = i,
                Name = $"Canned Account {i:00}"
            })
            .ToList();
    }

    private static List<PaymentPeriodEnd> BuildPaymentPeriodEnds()
    {
        var now = DateTime.UtcNow;
        return new List<PaymentPeriodEnd>
        {
            new()
            {
                Id = "2324-R11",
                CalendarPeriod = new CalendarPeriod { Month = 2, Year = 2024 },
                ReferenceData = new ReferenceData
                {
                    AccountDataValidAt = now.AddDays(-2),
                    CommitmentDataValidAt = now.AddDays(-1)
                },
                CompletionDateTime = now.AddHours(-4),
                Links = new Links { PaymentsForPeriod = "/api/payments/period/2324-R11" }
            },
            new()
            {
                Id = "2324-R12",
                CalendarPeriod = new CalendarPeriod { Month = 3, Year = 2024 },
                ReferenceData = new ReferenceData
                {
                    AccountDataValidAt = now.AddDays(-1),
                    CommitmentDataValidAt = now
                },
                CompletionDateTime = now.AddHours(-2),
                Links = new Links { PaymentsForPeriod = "/api/payments/period/2324-R12" }
            }
        };
    }

    private static List<PeriodEnd> BuildFinancePeriodEnds()
    {
        return new List<PeriodEnd>
        {
            new()
            {
                Id = 1,
                PeriodEndId = "2324-R10",
                CalendarPeriodMonth = 1,
                CalendarPeriodYear = 2024,
                AccountDataValidAt = DateTime.UtcNow.AddDays(-10),
                CommitmentDataValidAt = DateTime.UtcNow.AddDays(-9),
                CompletionDateTime = DateTime.UtcNow.AddDays(-8),
                PaymentsForPeriod = "/api/payments/period/2324-R10"
            }
        };
    }

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
