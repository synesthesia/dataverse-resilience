using DVConsole.Model.DTO;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;

namespace DVConsole.Services;

/// <summary>
/// <inheritdoc cref="IDataverseClient"/>
/// </summary>
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

    /// <summary>
    /// Get ID of Dataverse user
    /// </summary>
    /// <returns></returns>
    public async Task<Guid?> GetUserId()
    {
        var response = await _client.GetAsync("WhoAmI");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var whoAmIResponse = JsonConvert.DeserializeObject<WhoAmIResponse>(content);
        return whoAmIResponse?.UserId;
    }

    /// <summary>
    /// Create record
    /// </summary>
    /// <param name="entityCollection"></param>
    /// <param name="data"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<Guid> Create(
        string entityCollection,
        object data,
        CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, entityCollection);
        var jsonData = JsonConvert.SerializeObject(data);
        request.Content = new StringContent(jsonData, Encoding.UTF8, "application/json");

        var response = await _client.SendAsync(request, ct);

        await EnsureSuccessStatusCode(response, ct);
        var idGuid = GetEntityIdFromResponse(response);
        return idGuid;
    }

    
    /// <summary>
    /// Delete record
    /// </summary>
    /// <param name="entityCollection"></param>
    /// <param name="entityId"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task Delete(string entityCollection, Guid entityId, CancellationToken ct = default)
    {
     
        var response = await _client.DeleteAsync(
            $"{entityCollection}({entityId})",
            ct);

        await EnsureSuccessStatusCode(response, ct);
    }

    /// <summary>
    /// Helper method to check response for success
    /// </summary>
    /// <param name="response"></param>
    /// /// <param name="ct"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private async Task EnsureSuccessStatusCode(
        HttpResponseMessage response, 
        CancellationToken ct = default)
    {
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError(content);
            throw new InvalidOperationException("Request failed");
        }
    }

    /// <summary>
    /// Helper method to get ID from response
    /// </summary>
    /// <param name="response"></param>
    /// <returns></returns>
    private static Guid GetEntityIdFromResponse(HttpResponseMessage? response)
    {
        if (response?.Headers == null)
            return Guid.Empty;
        if (!response.Headers.Contains("OData-EntityId"))
            return Guid.Empty;

        var idString = response.Headers.GetValues("OData-EntityId").FirstOrDefault();

        if (string.IsNullOrEmpty(idString))
            return Guid.Empty;

        string[] entityIdSeps;
        entityIdSeps = new[] { "(", ")" };
        var entityIdParts = idString.Split(entityIdSeps, StringSplitOptions.None);

        if (entityIdParts.Length < 2)
            return Guid.Empty;

        //if alternate key was used to perform an upsert, guid not currently returned
        //the call returns the alternate key which is not in guid format
        Guid.TryParse(entityIdParts[1], out var idGuid);

        return idGuid;
    }
}