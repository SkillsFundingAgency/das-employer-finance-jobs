using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using NServiceBus;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Interfaces;
using SFA.DAS.Employer.Finance.Jobs.Infrastructure.Models;
using SFA.DAS.Employer.Finance.Messages.Commands;

namespace SFA.DAS.Employer.Finance.Jobs.Orchestrators;

public class ProcessPeriodEndOrchestrator(
    ILogger<ProcessPeriodEndOrchestrator> logger,
    IPeriodEndService periodEndService,
    IAccountService accountService,
    IFunctionEndpoint functionEndpoint)
{
    private const int PageSize = 10000;

    [Function(nameof(ProcessPeriodEndOrchestrator))]
    public async Task<PeriodEndResult> Run([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var input = context.GetInput<PeriodEnd>();

        if (input == null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        ValidateOrThrow(input, context);

        var periodEnd = await periodEndService.CreatePeriodEndAsync(input, context.NewGuid());

        var periodEndRef = periodEnd.PeriodEndId ?? periodEnd.Id.ToString();
        var totalCommandsPublished = await context.CallActivityAsync<int>(
            nameof(PublishAccountPaymentCommandsActivity),
            new PublishAccountPaymentCommandsInput { PeriodEndRef = periodEndRef, PeriodEnd = periodEnd });

        return new PeriodEndResult
        {
            PeriodEndId = periodEnd.Id.ToString(),
            TotalCommandsPublished = totalCommandsPublished
        };
    }

    [Function(nameof(PublishAccountPaymentCommandsActivity))]
    public async Task<int> PublishAccountPaymentCommandsActivity(
        [ActivityTrigger] PublishAccountPaymentCommandsInput input,
        FunctionContext executionContext,
        CancellationToken cancellationToken)
    {
        var periodEndRef = input.PeriodEndRef;
        logger.LogInformation(
            "PublishAccountPaymentCommandsActivity started for period end {PeriodEndRef}, fetching accounts from Finance API in pages of {PageSize}",
            periodEndRef,
            PageSize);

        var totalPublished = 0;
        var page = 1;

        while (true)
        {
            var pageInput = new GetAccountsRequest
            {
                Page = page,
                PageSize = PageSize,
                CorrelationId = Guid.NewGuid()
            };

            var accounts = await accountService.GetAccountsAsync(pageInput);

            if (accounts == null || accounts.Count == 0)
            {
                logger.LogInformation(
                    "PublishAccountPaymentCommandsActivity: no accounts returned for page {Page}, ending paged fetch",
                    page);
                break;
            }

            foreach (var account in accounts)
            {
                var command = new ImportAccountPaymentsCommand
                {
                    AccountId = account.Id,
                    PeriodEndRef = periodEndRef
                };

                var sendOptions = new SendOptions();
                sendOptions.SetMessageId($"{nameof(ImportAccountPaymentsCommand)}-{periodEndRef}-{account.Id}");

                await functionEndpoint.Send(command, sendOptions, executionContext, cancellationToken);
                totalPublished++;
            }

            logger.LogInformation(
                "PublishAccountPaymentCommandsActivity: published {Count} commands for page {Page} (total so far: {TotalPublished})",
                accounts.Count,
                page,
                totalPublished);

            if (accounts.Count < PageSize)
            {
                logger.LogInformation(
                    "PublishAccountPaymentCommandsActivity completed for period end {PeriodEndRef}: {TotalPublished} ImportAccountPaymentsCommands published across {TotalPages} pages",
                    periodEndRef,
                    totalPublished,
                    page);
                break;
            }

            page++;
        }

        return totalPublished;
    }

    private void ValidateOrThrow(PeriodEnd input, TaskOrchestrationContext context)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        if (input.AccountDataValidAt == default)
            throw new InvalidOperationException("AccountDataValidAt must be provided.");

        if (input.CommitmentDataValidAt == default)
            throw new InvalidOperationException("CommitmentDataValidAt must be provided.");

        var now = context.CurrentUtcDateTime;

        if (input.AccountDataValidAt > now)
            throw new InvalidOperationException("AccountDataValidAt cannot be in the future.");

        if (input.CommitmentDataValidAt > now)
            throw new InvalidOperationException("CommitmentDataValidAt cannot be in the future.");
    }
}
