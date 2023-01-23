
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.Client;

namespace DVConsole.HostedServices;

internal class DataverseConsoleExample01 : IHostedService
{
    private readonly ServiceClient _xrmService;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly ILogger _logger;

    public DataverseConsoleExample01(
        ServiceClient xrmService,
        IHostApplicationLifetime hostApplicationLifetime,
        ILogger<DataverseConsoleExample01> logger)
    {
        _xrmService = xrmService;
        _hostApplicationLifetime = hostApplicationLifetime;
        _logger = logger;
    }


    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Worker.StartAsync()");

        // do some work here
        _logger.LogInformation("Dataverse ServiceClient.IsReady: {0}", _xrmService.IsReady);

        
        _logger.LogCritical("Calling host to end application");
        _hostApplicationLifetime.StopApplication();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Worker.StopAsync()");
        return Task.CompletedTask;
    }
}