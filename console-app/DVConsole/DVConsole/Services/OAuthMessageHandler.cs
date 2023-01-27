using Microsoft.Identity.Client;
using System.Net.Http.Headers;

namespace DVConsole.Services;

/// <summary>
/// A delegating HTTP handler that authenticates to Dataverse using the Microsoft.Identity.Client library.
/// </summary>
/// <remarks>see https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/enhanced-quick-start</remarks>
public class OAuthMessageHandler : DelegatingHandler
{
    private readonly IConfidentialClientApplication _authProvider;
    private readonly string[] _scopes;

    public OAuthMessageHandler(
        string serviceUrl, 
        IConfidentialClientApplication authProvider,
        HttpMessageHandler innerHandler)
        : base(innerHandler)
    {
        _authProvider = authProvider;
        var scope = serviceUrl + "//.default";
        _scopes = new[] { scope };
    }

    private AuthenticationResult GetToken()
    {
        return _authProvider.AcquireTokenForClient(_scopes).ExecuteAsync().Result;
    }

    protected override  Task<HttpResponseMessage> SendAsync(
              HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
    {
        var token = GetToken(); 
        var authHeader = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        request.Headers.Authorization = authHeader;
        return base.SendAsync(request, cancellationToken);
    }

}