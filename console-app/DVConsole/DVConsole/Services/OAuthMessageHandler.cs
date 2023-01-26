using Microsoft.Identity.Client;
using System.Net.Http.Headers;
using System.Security;
using static System.Formats.Asn1.AsnWriter;

namespace DVConsole.Services;

// see https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/enhanced-quick-start
public class OAuthMessageHandler : DelegatingHandler
{
    private AuthenticationHeaderValue authHeader;
    private readonly IConfidentialClientApplication _authProvider;
    private readonly string[] _scopes;

    public OAuthMessageHandler(
        string serviceUrl, 
        string clientId, 
        string clientSecret, 
        string tenantId,
        HttpMessageHandler innerHandler)
        : base(innerHandler)
    {
        
        _authProvider = ConfidentialClientApplicationBuilder
            .Create(clientId)
            .WithClientSecret(clientSecret)
            .WithAuthority(AzureCloudInstance.AzurePublic, tenantId)
            .Build();
        
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
        authHeader = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        request.Headers.Authorization = authHeader;
        return base.SendAsync(request, cancellationToken);
    }

}