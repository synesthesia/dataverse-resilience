
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using System.Collections.Concurrent;
using System.Dynamic;
using DVConsole.Model.DTO;
using DVConsole.Services;

namespace DVConsole.HostedServices;

internal class DataverseConsoleExample04 : IHostedService
{
    private readonly IDataverseClient _xrmClient;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly ILogger _logger;
    private int _recordCounter = 0;

    public DataverseConsoleExample04(
        IDataverseClient xrmClient,
        IHostApplicationLifetime hostApplicationLifetime,
        ILogger<DataverseConsoleExample03> logger)
    {
        _xrmClient = xrmClient;
        _hostApplicationLifetime = hostApplicationLifetime;
        _logger = logger;
    }


#pragma warning disable CS1998
    public async Task StartAsync(CancellationToken cancellationToken)
#pragma warning restore CS1998
    {
        _logger.LogInformation("Worker.StartAsync()");

        OptimiseConnectionSettings();

        var startTime = DateTime.UtcNow;

        try
        {
            await CreateAndDeleteAccounts(3000);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in worker");
        }
        finally
        {
            var secondsForRun = (DateTime.Now - startTime).TotalSeconds;
            
            _logger.LogInformation($"Finished in {Math.Round(secondsForRun)} seconds, {_recordCounter} records created.");

            _logger.LogCritical("Calling host to end application");

            _hostApplicationLifetime.StopApplication();
        }
    }

    private void OptimiseConnectionSettings()
    {
        
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
        var accountsToCreate = new List<object>();
        var count = 0;
        while (count < numberOfAccounts)
        {
            dynamic account = new ExpandoObject();
            account.name = $"Account {count}";
            accountsToCreate.Add(account);
            count++;
        }

        var createdIds = new ConcurrentBag<Guid>();

        try
        {
            _logger.LogInformation($"Creating and deleting {accountsToCreate.Count} accounts");

            var startCreate = DateTime.Now;

            var userId = await _xrmClient.GetUserId();

            _logger.LogInformation($"UserId: {userId}");

            var parallelOptions = new ParallelOptions()
            {
                MaxDegreeOfParallelism = 12
            };
            
            await Parallel.ForEachAsync(
                source: accountsToCreate,
                parallelOptions: parallelOptions,
                async (entity, token) =>
                {
                    createdIds.Add(await _xrmClient.Create("accounts", entity, token));
                    Interlocked.Increment(ref _recordCounter);
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
                     await _xrmClient.Delete("accounts", id, token);
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