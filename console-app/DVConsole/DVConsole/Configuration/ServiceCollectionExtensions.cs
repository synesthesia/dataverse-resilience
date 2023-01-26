using System.Net.Http.Headers;
using DVConsole.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;

namespace DVConsole.Configuration
{
    public static class ServiceCollectionExtensions
    {
        public enum DataverseConnectionMode
        {
            UserLogin,
            ClientSecret
        }

        public static IServiceCollection UseDataVerse(
            this IServiceCollection services,
            DataverseConnectionMode mode = DataverseConnectionMode.UserLogin
            )
        {
            switch (mode)
            {
                case DataverseConnectionMode.UserLogin:
                    services.UseDataVerseUserLogin();
                    break;
                case DataverseConnectionMode.ClientSecret:
                    services.UseDataVerseClientSecret();
                    break;
            }

            return services;
        }

        public static IServiceCollection UseDataVerseUserLogin(this IServiceCollection services)
        {

            ServiceClient ServiceFactory(IServiceProvider sp)
            {
                var options = sp.GetRequiredService<IOptions<DataVerseOptions>>();
                var config = options.Value;
                var logger = sp.GetService<ILogger<ServiceClient>>();

                var instanceUrl = config.InstanceUrl;

                var connString = $@"
                AuthType = OAuth;
                Url = {instanceUrl};
                ClientId = {config.ClientId};
                RedirectUri = http://localhost;
                RequireNewInstance = false;
                TokenCacheStorePath = c:\MyTokenCache\msal_cache.data;
                LoginPrompt = Auto";
                //RedirectUri = app://{config.ClientId};
                //RedirectUri = https://login.microsoftonline.com/common/oauth2/nativeclient;
                return new ServiceClient(connString, logger: logger);
            }
            services.AddSingleton<ServiceClient>(ServiceFactory);
            services.AddSingleton<IOrganizationService>(sp => sp.GetService<ServiceClient>());
            services.AddSingleton<IOrganizationServiceAsync2>(sp => sp.GetService<ServiceClient>());

            return services;
        }

        public static IServiceCollection UseDataVerseClientSecret(this IServiceCollection services)
        {

            ServiceClient ServiceFactory(IServiceProvider sp)
            {
                var options = sp.GetRequiredService<IOptions<DataVerseOptions>>();
                var config = options.Value;
                var logger = sp.GetService<ILogger<ServiceClient>>();

                var instanceUrl = config.InstanceUrl;

                var connString = $@"
                AuthType = ClientSecret;
                Url = {instanceUrl};
                ClientId = {config.ClientId};
                ClientSecret = {config.ClientSecret};
                RedirectUri = http://localhost;
                RequireNewInstance = false;
                TokenCacheStorePath = c:\MyTokenCache\msal_cache.data;
                LoginPrompt = Auto";
                
                //RedirectUri = app://{config.ClientId};
                //RedirectUri = https://login.microsoftonline.com/common/oauth2/nativeclient;
                return new ServiceClient(connString, logger:logger);
            }
            services.AddSingleton<ServiceClient>(ServiceFactory);
            services.AddSingleton<IOrganizationService>(sp => sp.GetService<ServiceClient>());
            services.AddSingleton<IOrganizationServiceAsync2>(sp => sp.GetService<ServiceClient>());

            return services;
        }

        public static IServiceCollection UseDataVerseHttpClient(
            this IServiceCollection services)
        {

            services.AddSingleton<IConfidentialClientApplication>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<DataVerseOptions>>();
                var config = options.Value;


                var app = ConfidentialClientApplicationBuilder
                    .Create(config.ClientId)
                    .WithClientSecret(config.ClientSecret)
                    .WithAuthority(AzureCloudInstance.AzurePublic, config.TenantId)
                    .Build();
                
                return app;
            });



            services.AddHttpClient<IDataverseClient, DataverseClient>(
                    (sp, client) =>
                    {
                        var options = sp.GetRequiredService<IOptions<DataVerseOptions>>();
                        var config = options.Value;

                        // Set the base address of the named client.
                        client.BaseAddress = new Uri(config.InstanceUrl + "/api/data/v9.2/");

                        var headers = client.DefaultRequestHeaders;

                        // Add a user-agent default request header.
                        headers.UserAgent.ParseAdd("dotnet-docs");

                        headers.Add("OData-MaxVersion", "4.0");
                        headers.Add("OData-Version", "4.0");
                        headers.Accept.Add(
                            new MediaTypeWithQualityHeaderValue("application/json"));

                    })
                .ConfigureHttpMessageHandlerBuilder(builder =>
                {
                    builder.PrimaryHandler = builder.Services.GetRequiredService<OAuthMessageHandler>();
                    
                });


            services.AddSingleton<IConfidentialClientApplication>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<DataVerseOptions>>();
                var config = options.Value;
                
                var authProvider = ConfidentialClientApplicationBuilder
                    .Create(config.ClientId)
                    .WithClientSecret(config.ClientSecret)
                    .WithAuthority(AzureCloudInstance.AzurePublic, config.TenantId)
                    .Build();

                return authProvider;
            });

            
            services.AddTransient<OAuthMessageHandler>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<DataVerseOptions>>();
                var config = options.Value;
                var ap = sp.GetRequiredService<IConfidentialClientApplication>();

                var handler = new OAuthMessageHandler(
                    config.InstanceUrl,
                    ap,
                    new HttpClientHandler() {UseCookies = false});
                
                return handler;
            });

            return services;
        }
    }
}