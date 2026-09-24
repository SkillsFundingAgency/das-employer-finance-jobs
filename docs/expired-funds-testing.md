# Testing the combined expired-funds changes

Branch: `Appman-ExpiredFundsMerged`, starting from PR #42 (`APPMAN-2975`) and merging PR #45 (`APPMAN-2976`) and PR #46 (`APPMAN-3140`).

The complete flow is: monthly timer -> paged account retrieval -> expiry API call for each account -> `AccountFundsExpiredEvent` for accounts where funds expired -> final orchestration summary.

## 1. Prepare the environment

1. Run the Azure DevOps pipeline against **Appman-ExpiredFundsMerged** and deploy its build to an agreed test environment. The YAML push trigger includes only `main`, so explicitly select this branch for a manual run. Confirm the deployed build commit matches the combined branch.
2. Check the deployment output `ExpireFundsFunctionAppName`. It identifies the new app ending in **`-exp-fa`**. Confirm its package is `SFA.DAS.Employer.Finance.Jobs.Functions.ExpireFunds.zip`, the runtime is .NET 10 isolated / Functions v4, and all five functions are indexed:
   - `ExpireFundsTimer`
   - `ExpireFundsOrchestrator`
   - `GetAccountsPageActivity`
   - `ProcessAccountExpireFundsActivity`
   - `PublishAccountFundsExpiredEventActivity`
3. Confirm the app can read its configuration (`ConfigNames=SFA.DAS.EmployerFinance.Jobs_2.0`), use `AzureWebJobsStorage`, and send telemetry to its own Application Insights resource. Its Durable task hub is **`ExpireFundsHub`**.
4. Confirm `FinanceApiConfiguration.Url` and `FinanceApiConfiguration.Identifier` point to the intended Employer Finance API and that the app's identity has the required API access. The API must support both `GET api/accounts?pageNumber=1&pageSize=...` and `POST api/accounts/{accountId}/expire-funds`. This repository calls that API; it does not implement the expiry calculation or database changes.
5. Confirm `AzureWebJobsServiceBus__fullyQualifiedNamespace` points to the test Service Bus namespace. The deployment adds a Service Bus role assignment for the expiry app. Check it succeeded, the NServiceBus endpoint starts, and any required NServiceBus licence is available. The endpoint name is `SFA.DAS.EmployerFinance.Jobs.ExpireFunds`.
6. Arrange an existing event subscriber or a dedicated test subscription **before** triggering the run, so published events can be captured. The app uses NServiceBus `TopicTopology.Default`; inspect the configured subscriber/topic routing rather than expecting an expiry-named input queue. Also obtain access to API logs/data and Durable instance output/history.
7. For a small, controlled pagination test, set the effective configuration section below. Confirm the actual values in the timer's startup log, because shared table configuration is loaded after environment settings.

Before deployment, also check that the selected environment's pipeline variable groups provide `EmployerFinanceJobsApimUrl` and the secret `EmployerFinanceJobsApimSubscriptionKey`. The shared `das-employer-config` schema added these required Outer API settings on 21 September 2026, with no defaults. The deployment template explicitly passes both values to Generate Configuration. A missing value can prevent deployment even though the expiry workflow itself calls the Finance API directly. This applies to AT and DEMO as well as any other selected environment.

```json
{
  "ExpireFundsOptions": {
    "AccountPageSize": 2,
    "MaxConcurrentAccounts": 1
  }
}
```

The production defaults are page size **1000** and concurrency **50**. Values <= 0 fall back to defaults; values above **10000** / **100** are capped respectively.

Use an isolated test dataset or a controlled API stub: the job processes **every account returned by the API**, with no account-ID filter. The existing `FakeServers` project has no `/expire-funds` mapping, so it needs additional mappings before it can support this test. A stub verifies orchestration and messaging; use the real Finance API to verify actual financial records and balances.

## 2. Prepare data and capture a baseline

Prepare at least three accounts through the Finance API team's supported fixtures/data setup:

| Account | Starting state | Expected first-run outcome |
| --- | --- | --- |
| A | Eligible long-term funds, with known expected amounts/counts | API reports `FundsExpired=true`; expected expiry records/balance changes; event published |
| B | Eligible short-term funds, with known expected amounts/counts | API reports `FundsExpired=true`; expected expiry records/balance changes; event published |
| C | No funds eligible for expiry, including funds not yet eligible | API reports `FundsExpired=false`; no expiry changes and no event |

Also cover an account with both types of eligible funds: expect **one account event**, not one event per expired fund. Record account IDs, eligible and ineligible amounts, expected expiry counts, balances and existing expiry records before running. Eligibility dates, business rules and table names belong to the Finance API implementation and should come from its test fixtures.

For a stub, an example successful expiry response is:

```json
{
  "accountId": 1001,
  "correlationId": "<echo the request correlationId>",
  "fundsExpired": true,
  "longTermExpiredFundsCount": 1,
  "shortTermExpiredFundsCount": 0
}
```

Return account pages in a stable order, honour `pageNumber`/`pageSize`, and return an `accounts` array. Control transient failures per account and per attempt for the resilience tests below.

## 3. Trigger the complete flow

The timer schedule is `0 0 0 28 * *` (midnight on the 28th in the host's configured timezone; normally UTC), with `RunOnStartup=false`. Deployment defaults `AzureWebJobs.ExpireFundsTimer.Disabled` to **true**. Do not wait for the scheduled date: manually invoke **ExpireFundsTimer** in the new expiry app.

In Azure, use Functions -> ExpireFundsTimer -> Code + Test -> Test/Run with the app's master key, or make this request from a client that can reach the app:

```http
POST https://<expiry-function-app-hostname>/admin/functions/ExpireFundsTimer
x-functions-key: <master key from the test app>
Content-Type: application/json

{}
```

Expect **202 Accepted**, then follow the logs. The master key allows manual execution while the timer remains disabled. See Microsoft's [manual trigger instructions](https://learn.microsoft.com/en-us/azure/azure-functions/functions-manually-run-non-http) and [disabled function behavior](https://learn.microsoft.com/en-us/azure/azure-functions/disable-function). Keep the key out of saved test evidence.

Capture the timer's **CorrelationId** and look for instance **`ExpireFundsOrchestrator-Singleton`** in **`ExpireFundsHub`**. The timer creates a new correlation ID for each invocation; the accepted run's ID is the one to follow. Triggering an individual activity does not test the complete flow.

## 4. Verify the successful run

| Where to look | What to verify |
| --- | --- |
| Expiry app startup/deployment | All five functions are present; no dependency-injection, API authentication, NServiceBus startup or deployment-name errors |
| Finance API requests | Pages start at 1; configured page size is used; each discovered account reaches `POST api/accounts/{id}/expire-funds` with the run's `CorrelationId` in the body |
| Finance API response and data | Eligible records and balances change by the expected amounts; ineligible records do not change; logged long-term/short-term counts agree with API results |
| Activity and publisher logs | Account ID and correlation ID connect API processing to event publication; no event is published for `FundsExpired=false` or failed expiry |
| Test subscriber / Service Bus capture | `SFA.DAS.EmployerFinance.Messages.Events.AccountFundsExpiredEvent` has the correct `AccountId` and UTC `Created`; NServiceBus correlation header matches the run; transport message ID matches the publisher log |
| Durable output and final summary | `Success=true`; all discovered accounts processed; zero failures; successful and funds-expired counts match the dataset |

For exactly A/B/C above, with page size 2, expect:

```text
PagesProcessed=2, TotalAccountsCount=3, ProcessedAccountsCount=3,
SuccessfulAccountsCount=3, FailedAccountsCount=0,
FundsExpiredAccountsCount=2, Success=true
```

`FundsExpiredAccountsCount` counts **accounts**, not individual funds or messages. If the last nonempty page is full, an extra empty page is fetched and counted. An empty first page produces `PagesProcessed=1`, all account counts zero and `Success=true`.

**A Durable runtime status of `Completed` is not sufficient to pass.** The orchestrator catches failures and returns an output object; inspect `Success`, all counters, `ErrorMessage`, per-account warnings and publication errors. Account-level failures can leave the top-level `ErrorMessage` empty.

Example query in the expiry app's Application Insights Logs view:

```kusto
let runCorrelationId = "<accepted run correlation ID>";
traces
| where timestamp > ago(24h)
| where message contains runCorrelationId
    or tostring(customDimensions) contains runCorrelationId
| project timestamp, severityLevel, message, customDimensions
| order by timestamp asc
```

Also inspect exceptions and Finance API telemetry for that time window. Trace sampling is enabled in `host.json`, so use Durable history, API evidence and captured messages to substantiate counts rather than relying solely on the number of log entries.

## 5. Exercise resilience and isolation

Use controlled fault injection for these cases; keep another successful account in the same run to prove processing continues.

| Scenario | How to exercise | Expected result |
| --- | --- | --- |
| Expiry transient failure, then recovery | Return 503 or 429 for one account, then a normal response | Expiry activity retries, then succeeds; one logical event if funds expired |
| Expiry retries exhausted | Keep returning 503 for one account | Up to **3 total activity attempts**, first retry interval **5 seconds**; account fails, no event for it, other accounts continue, overall `Success=false` |
| Expiry permanent failure | Return 400/404 for one account | No Durable retry for this account; failed-account count increases; no event; other accounts continue |
| Missing expiry response body | Return a successful status without the required JSON body | Account fails; no event; other accounts continue |
| Publishing transient failure, then recovery | Inject a transient `ServiceBusException`/timeout at the publisher, then recover | Only the publish activity retries (up to 3 total attempts, first interval 5 seconds); the already completed expiry activity is not rerun by this publication retry |
| Publishing failure persists | Exhaust transient publish retries, or inject a permanent publisher error | Account marked failed, `FundsExpiredAccountsCount` still includes it, overall `Success=false`, other accounts continue; log says event publication failed |
| Account-page retrieval failure | Fail a page request throughout its retry allowance | Page activity retries; on exhaustion the run ends with `Success=false` and `ErrorMessage`; earlier processed counts remain, subsequent pages are not processed |
| Concurrency | Use slow responses, page size >= 3 and concurrency 1, then 2 | No more than the configured number of account workflows are in flight; publication is part of each workflow; check scheduling and concurrency-limit logs |
| Singleton protection | While the instance is Running, Pending or Suspended, invoke the timer again | Log reports the orchestrator is already running; no additional orchestration or duplicate account fan-out is started |
| Empty account list | Return `accounts: []` on page 1 | Successful empty run; no expiry calls/events |
| Rerun after completion | Run again with the same real dataset once the first run is finished | Verify the Finance API does not expire the same funds twice; whenever it returns `FundsExpired=false`, no new event is sent |
| Host restart during a run | Restart the test app while work is in progress | Durable history resumes; verify API state and subscriber processing for unintended duplicates |

The expiry transient status list is 408, 429, 500, 502, 503 and 504; network `HttpRequestException`, timeout and task cancellation are also treated as transient. Underlying HTTP/Service Bus clients can have their own retries, so distinguish Durable activity attempts from raw transport attempts. Invalid Service Bus configuration can prevent startup; it is not a reliable way to test a per-account publishing failure. Use a controlled publisher fault after successful startup.

Event publication uses a deterministic message ID based on **correlation ID + account ID** and preserves the event timestamp across retries of that activity. Verify those values remain stable. This branch does not establish exactly-once delivery: confirm the environment's broker duplicate-detection and consumer deduplication behavior. A new timer run creates a new correlation ID and therefore a different message ID.

If expiry succeeds but publication permanently fails, retain that account as a failed test outcome. A fresh full run is not a guaranteed repair: if the API now returns `FundsExpired=false`, the event will not be published. Capture the account ID/correlation ID/message ID for recovery investigation.

## 6. Automated verification and handover

With the .NET 10 SDK, run from the repository root:

```powershell
dotnet test src/SFA.DAS.Employer.Finance.Jobs.sln --configuration Release
```

Existing tests cover options, timer/singleton behavior, paging, concurrency, activity error classification, event payload/headers/IDs, publication failures and dependency registration. These are unit tests with mocked dependencies; deployed timer execution, real Durable retries, Finance API persistence and Service Bus delivery still need the checks above.

Attach the deployed build/commit, test account baselines and after-state, accepted correlation ID, Durable output/history, API results, captured events/headers and fault-injection outcomes to the test record. Record failures separately from successes. Check that the existing payments and levy apps still start after deployment. Restore temporary page/concurrency settings and leave the timer in the agreed enabled/disabled state after testing.
