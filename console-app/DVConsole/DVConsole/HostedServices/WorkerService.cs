using DVConsole.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DVConsole.HostedServices;

internal class WorkerService : IHostedService
{
    private readonly ITestInterface _testService;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly ILogger _logger;

    public WorkerService(
        ITestInterface testService,
        IHostApplicationLifetime hostApplicationLifetime,
        ILogger<WorkerService> logger)
    {
        _testService = testService;
        _hostApplicationLifetime = hostApplicationLifetime;
        _logger = logger;
    }


    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Worker.StartAsync()");
        _testService.Foo();
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