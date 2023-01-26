
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Rest;
using Microsoft.Xrm.Sdk;
using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace DVConsole.HostedServices;

internal class DataverseConsoleExample02 : IHostedService
{
    private readonly ServiceClient _xrmService;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly ILogger _logger;

    public DataverseConsoleExample02(
        ServiceClient xrmService,
        IHostApplicationLifetime hostApplicationLifetime,
        ILogger<DataverseConsoleExample01> logger)
    {
        _xrmService = xrmService;
        _hostApplicationLifetime = hostApplicationLifetime;
        _logger = logger;
    }


#pragma warning disable CS1998
    public async Task StartAsync(CancellationToken cancellationToken)
#pragma warning restore CS1998
    {
        _logger.LogInformation("Worker.StartAsync()");

        _logger.LogInformation("Dataverse ServiceClient.IsReady: {0}", _xrmService.IsReady);
        _logger.LogInformation("Dataverse recommended max parallelism : {0}", _xrmService.RecommendedDegreesOfParallelism);
        
        OptimiseConnectionSettings();

        var startTime = DateTime.UtcNow;
        
        await CreateAndDeleteAccounts(1000);
        
        var secondsForRun = (DateTime.Now - startTime).TotalSeconds;

        _logger.LogInformation($"Finished in {Math.Round(secondsForRun)} seconds.");
        
        _logger.LogCritical("Calling host to end application");
        
        _hostApplicationLifetime.StopApplication();
    }

    private void OptimiseConnectionSettings()
    {
        //Disable affinity cookie - this is a cookie that is used to route requests to the same server in a web farm
        _xrmService.EnableAffinityCookie = false;

        //Change max connections from .NET to a remote service default: 2
        System.Net.ServicePointManager.DefaultConnectionLimit = 65000;
        //Bump up the min threads reserved for this app to ramp connections faster - minWorkerThreads defaults to 4, minIOCP defaults to 4
        System.Threading.ThreadPool.SetMinThreads(100, 100);
        //Turn off the Expect 100 to continue message - 'true' will cause the caller to wait until it round-trip confirms a connection to the server
        System.Net.ServicePointManager.Expect100Continue = false;
        //Can decrease overall transmission overhead but can cause delay in data packet arrival
        System.Net.ServicePointManager.UseNagleAlgorithm = false;
    }

    // see https://learn.microsoft.com/en-us/power-apps/developer/data-platform/send-parallel-requests?tabs=sdk
    private async Task CreateAndDeleteAccounts(int numberOfAccounts)
    {
        var accountsToCreate = new List<Entity>();
        var count = 0;
        while (count < numberOfAccounts)
        {
            var account = new Entity("account")
            {
                ["name"] = $"Account {count}"
            };
            accountsToCreate.Add(account);
            count++;
        }

        var createdIds = new ConcurrentBag<Guid>();

        try
        {
            _logger.LogInformation($"Creating and deleting {accountsToCreate.Count} accounts");

            var startCreate = DateTime.Now;

            var parallelOptions = new ParallelOptions()
            {
                MaxDegreeOfParallelism =
                    _xrmService.RecommendedDegreesOfParallelism
            };

            await Parallel.ForEachAsync(
                source: accountsToCreate,
                parallelOptions: parallelOptions,
                async (entity, token) =>
                {
                    createdIds.Add(await _xrmService.CreateAsync(entity, token));
                });

            var secondsToCreate = (DateTime.Now - startCreate).TotalSeconds;

            _logger.LogInformation($"Created {accountsToCreate.Count} accounts in  {Math.Round(secondsToCreate)} seconds.");

            _logger.LogInformation($"Deleting {createdIds.Count} accounts");
            var startDelete = DateTime.Now;

            await Parallel.ForEachAsync(
                source: createdIds,
                parallelOptions: parallelOptions,
                async (id, token) =>
                {
                    await _xrmService.DeleteAsync("account", id, token);
                });

            var secondsToDelete = (DateTime.Now - startDelete).TotalSeconds;

            _logger.LogInformation($"Deleted {createdIds.Count} accounts in {Math.Round(secondsToDelete)} seconds.");

        }
        catch (AggregateException ae)
        {
            var inner = ae.InnerExceptions.FirstOrDefault();
            if (inner != null)
            {
                throw inner;
            }
        }

    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Worker.StopAsync()");
        return Task.CompletedTask;
    }
}