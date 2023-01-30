using System.Net.Http.Headers;
using DVConsole.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Polly;
using Polly.Extensions.Http;

namespace DVConsole.Configuration
{
    public static class ServiceCollectionExtensions
    {
        public enum DataverseConnectionMode
        {
            UserLogin,
            ClientSecret
        }

        /// <summary>
        /// Configure the App to use Dataverse ServiceClient
        /// via either client secret or interactive login
        /// </summary>
        /// <param name="services"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
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
                return new ServiceClient(connString, logger: logger);
            }
            services.AddSingleton<ServiceClient>(ServiceFactory);
            services.AddSingleton<IOrganizationService>(ServiceFactory);
            services.AddSingleton<IOrganizationServiceAsync2>(ServiceFactory);

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
                
                return new ServiceClient(connString, logger:logger);
            }
            services.AddSingleton<ServiceClient>(ServiceFactory);
            services.AddSingleton<IOrganizationService>(ServiceFactory);
            services.AddSingleton<IOrganizationServiceAsync2>(ServiceFactory);

            return services;
        }

        /// <summary>
        /// Configure the app to use HttpClient to access Dataverse
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static IServiceCollection UseDataVerseHttpClient(
            this IServiceCollection services)
        {

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
                    builder.PrimaryHandler =
                        builder.Services.GetRequiredService<OAuthMessageHandler>();
                })
                .AddPolicyHandler((s, request) =>
                    HttpPolicyExtensions.HandleTransientHttpError()
                        .OrResult(httpResponseMessage => httpResponseMessage.StatusCode ==
                                                         System.Net.HttpStatusCode.TooManyRequests)
                        .WaitAndRetryAsync<HttpResponseMessage>(
                            retryCount: 5,
                            sleepDurationProvider: (count, response, context) =>
                            {
                                int seconds;
                                var headers = response.Result.Headers;
                                if (headers.Contains("Retry-After"))
                                {
                                    seconds = int.Parse(headers.GetValues("Retry-After")
                                        .FirstOrDefault());
                                }
                                else
                                {
                                    seconds = (int) Math.Pow(2, count);
                                }

                                return TimeSpan.FromSeconds(seconds);
                            },
                            onRetryAsync: (outcome, timespan, retryAttempt, context) =>
                            {
                                context["RetriesInvoked"] = retryAttempt;
                                s.GetService<ILogger<DataverseClient>>()?
                                    .LogWarning("Status {statusCode} - delaying for {delay}ms, then do retry {retry}.",
                                        outcome.Result.StatusCode,
                                        timespan.TotalMilliseconds, 
                                        retryAttempt);
                                return Task.CompletedTask;
                            })
                );


            services.AddTransient<OAuthMessageHandler>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<DataVerseOptions>>();
                var config = options.Value;

                if (config?.InstanceUrl == null)
                {
                    throw new InvalidOperationException(
                        "InstanceUrl is not set in the configuration");
                }
                var ap = sp.GetRequiredService<IConfidentialClientApplication>();

                var handler = new OAuthMessageHandler(
                    config.InstanceUrl,
                    ap,
                    new HttpClientHandler() {UseCookies = false});
                
                return handler;
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


            
            return services;

            
        }

        

        // <summary>
        /// Specifies the Retry policies
        /// </summary>
        /// <param name="config">Configuration data for the service</param>
        /// <returns></returns>
        static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(IOptions<PollyOptions> opts)
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(httpResponseMessage => httpResponseMessage.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                .WaitAndRetryAsync(
                    retryCount: opts.Value?.MaxRetries ?? 5,
                    sleepDurationProvider: (count, response, context) =>
                    {
                        int seconds;
                        var headers = response.Result.Headers;

                        if (headers.Contains("Retry-After"))
                        {
                            seconds = int.Parse(headers.GetValues("Retry-After").FirstOrDefault());
                        }
                        else
                        {
                            seconds = (int)Math.Pow(2, count);
                        }
                        return TimeSpan.FromSeconds(seconds);
                    },
                    onRetryAsync: (_, _, _, _) => { return Task.CompletedTask; }
                );
        }
    }
}