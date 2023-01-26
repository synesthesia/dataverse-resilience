using System.Net.Http.Headers;
using DVConsole.Configuration;
using DVConsole.Model.DTO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Newtonsoft.Json;

namespace DVConsole.Services;

public class DataverseClient : IDataverseClient
{
    private readonly IConfidentialClientApplication _authProvider;
    private HttpClient _client;
    private ILogger<DataverseClient> _logger;
    private readonly string _resource;
    private string[] _scopes;

    public DataverseClient(
        HttpClient httpClient,
        IConfidentialClientApplication authProvider,
        IOptions<DataVerseOptions> options,
        ILogger<DataverseClient> logger)
    {
        _client = httpClient;
        _authProvider = authProvider;
        _logger = logger;
        _resource = options.Value.InstanceUrl;
        var scope = _resource + "/.default";
        _scopes = new[] { scope };
    }

    public async Task<Guid?> GetUserId()
    {
        await Authenticate();
        var response = await _client.GetAsync("WhoAmI");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var whoAmIResponse = JsonConvert.DeserializeObject<WhoAmIResponse>(content);
        return whoAmIResponse?.UserId;
    }

    private async Task Authenticate()
    {
        
        var authResult =  await 
            _authProvider.AcquireTokenForClient(_scopes).ExecuteAsync();
        _client.DefaultRequestHeaders.Authorization 
            = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
    }
    
}