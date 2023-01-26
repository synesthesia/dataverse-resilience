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
    private HttpClient _client;
    private ILogger<DataverseClient> _logger;

    public DataverseClient(
        HttpClient httpClient,
        ILogger<DataverseClient> logger)
    {
        _client = httpClient;
        _logger = logger;
       
    }

    public async Task<Guid?> GetUserId()
    {
        var response = await _client.GetAsync("WhoAmI");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var whoAmIResponse = JsonConvert.DeserializeObject<WhoAmIResponse>(content);
        return whoAmIResponse?.UserId;
    }

   
}