
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

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


#pragma warning disable CS1998
    public async Task StartAsync(CancellationToken cancellationToken)
#pragma warning restore CS1998
    {
        _logger.LogInformation("Worker.StartAsync()");

        _logger.LogInformation("Dataverse ServiceClient.IsReady: {0}", _xrmService.IsReady);
        _logger.LogInformation("Dataverse recommended max parallelism : {0}", _xrmService.RecommendedDegreesOfParallelism);

        OptimiseConnectionSettings();

        var startTime = DateTime.UtcNow;
        
        CreateAndDeleteAccounts(10000);
        
        var secondsForRun = (DateTime.Now - startTime).TotalSeconds;

        _logger.LogInformation($"Finished in {Math.Round(secondsForRun)} seconds.");
        
        _logger.LogCritical("Calling host to end application");
        
        _hostApplicationLifetime.StopApplication();
    }

    private static void OptimiseConnectionSettings()
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

    // see https://learn.microsoft.com/en-us/power-apps/developer/data-platform/xrm-tooling/sample-tpl-crmserviceclient
    private void CreateAndDeleteAccounts(int numberOfAccounts)
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

        try
        {
            _logger.LogInformation($"Creating {accountsToCreate.Count} accounts");

            var startCreate = DateTime.Now;

            //Import the list of accounts
            var createdAccounts = CreateEntities(accountsToCreate);

            var secondsToCreate = (DateTime.Now - startCreate).TotalSeconds;

            _logger.LogInformation($"Created {accountsToCreate.Count} accounts in  {Math.Round(secondsToCreate)} seconds.");

            _logger.LogInformation($"Deleting {createdAccounts.Count} accounts");
            var startDelete = DateTime.Now;

            //Delete the list of accounts created
            DeleteEntities(createdAccounts.ToList());

            var secondsToDelete = (DateTime.Now - startDelete).TotalSeconds;

            _logger.LogInformation($"Deleted {createdAccounts.Count} accounts in {Math.Round(secondsToDelete)} seconds.");

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

    private ConcurrentBag<EntityReference> CreateEntities(List<Entity> entities)
    {
        var createdEntityReferences = new ConcurrentBag<EntityReference>();

        Parallel.ForEach(entities,
            new ParallelOptions() { MaxDegreeOfParallelism = _xrmService.RecommendedDegreesOfParallelism },
            () => _xrmService.Clone(), //Clone the ServiceClient for each thread
            (entity, loopState, index, threadLocalSvc) =>
            {
                // In each thread, create entities and add them to the ConcurrentBag
                // as EntityReferences
                createdEntityReferences.Add(
                    new EntityReference(
                        entity.LogicalName,
                        threadLocalSvc.Create(entity)
                    )
                );

                return threadLocalSvc;
            },
            (threadLocalSvc) =>
            {
                //Dispose the cloned ServiceClient instance
                if (threadLocalSvc != null)
                {
                    threadLocalSvc.Dispose();
                }
            });

        //Return the ConcurrentBag of EntityReferences
        return createdEntityReferences;
    }

    private void DeleteEntities(List<EntityReference> entityReferences)
    {
        Parallel.ForEach(entityReferences,
            new ParallelOptions() { MaxDegreeOfParallelism = _xrmService.RecommendedDegreesOfParallelism },
            () => _xrmService.Clone(), //Clone the ServiceClient for each thread
            (er, loopState, index, threadLocalSvc) =>
            {
                // In each thread, delete the entities
                threadLocalSvc.Delete(er.LogicalName, er.Id);

                return threadLocalSvc;
            },
            (threadLocalSvc) =>
            {
                //Dispose the cloned CrmServiceClient instance
                if (threadLocalSvc != null)
                {
                    threadLocalSvc.Dispose();
                }
            });
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Worker.StopAsync()");
        return Task.CompletedTask;
    }
}