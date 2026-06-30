using LLMConnect.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LLMConnect.Configuration;

/// <summary>
/// 
/// </summary>
public static class ServiceCollectionExtensions
{


    /// <summary>
    /// Adds the LLMConnect client to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration delegate for <see cref="LLMConnectClientOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddLLMConnect(
        this IServiceCollection services,
        Action<LLMConnectClientOptions>? configure = null)
    {
        if (configure != null)
            services.Configure(configure);
        else
            services.Configure<LLMConnectClientOptions>(_ => { }); // creates a new one with default options, validation will handle this.

        services.AddHttpClient("LLMConnect")
            .AddHttpMessageHandler(sp =>
            {
                var options = sp.GetRequiredService<IOptions<LLMConnectClientOptions>>().Value;
                var logger = options.LoggerFactory?.CreateLogger("LLMConnect.Retry");
                return new RetryDelegatingHandler(options.MaxRetries, logger);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(5)
            });

        services.AddSingleton<ILLMConnectClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<LLMConnectClientOptions>>().Value;
            var factory = sp.GetRequiredService<IHttpClientFactory>();

            return new LLMConnectClient(options, factory);
        });

        return services;
    }
}