using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        
    }
}