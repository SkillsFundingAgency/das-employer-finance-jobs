using Microsoft.Extensions.Hosting;
using NServiceBus;

namespace SFA.DAS.Employer.Finance.Jobs.Infrastructure.Services;

public class NServiceBusEndpointHostedService(IEndpointInstance endpointInstance) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => endpointInstance.Stop();
}
