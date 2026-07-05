using LLMConnect.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;

namespace LLMConnect.Configuration;

/// <summary>
/// Extension methods for registering LLMConnect services in an IServiceCollection.
/// </summary>
public static class ServiceCollectionExtensions
{

    private static readonly object _lock = new object();
    private static bool _coreServicesRegistered = false;

    /// <summary>
    /// Adds LLMConnect services to the IServiceCollection with a single configuration action for LLMConnectClientOptions.
    /// </summary>
    /// <param name="services">The IServiceCollection to add services to.</param>
    /// <param name="configure">The action to configure the LLMConnectClientOptions.</param>
    /// <returns>The IServiceCollection with the added services.</returns>
    public static IServiceCollection AddLLMConnect(
        this IServiceCollection services,
        Action<LLMConnectClientOptions>? configure = null)
    {
        services.Configure(configure ?? (_ => { }));

        RegisterCoreServices(services);

        services.AddSingleton<ILLMConnectClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<LLMConnectClientOptions>>().Value;
            return new LLMConnectClient(options);
        });

        return services;
    }



    /// <summary>
    /// Adds LLMConnect services to the IServiceCollection with separate configuration for general and endpoint options.
    /// </summary>
    /// <param name="services">The IServiceCollection to add services to.</param>
    /// <param name="configureGeneral">The action to configure the general options.</param>
    /// <param name="configureEndpoint">The action to configure the endpoint options.</param>
    /// <returns>The IServiceCollection with the added services.</returns>
    public static IServiceCollection AddLLMConnect(
        this IServiceCollection services,
        Action<LLMConnectGeneralOptions> configureGeneral,
        Action<LLMConnectEndpointOptions> configureEndpoint)
    {
        services.Configure(configureGeneral);
        services.Configure(configureEndpoint);

        RegisterCoreServices(services);

        services.AddSingleton<ILLMConnectClient>(sp =>
        {
            var general = sp.GetRequiredService<IOptions<LLMConnectGeneralOptions>>().Value;
            var endpoint = sp.GetRequiredService<IOptions<LLMConnectEndpointOptions>>().Value;

            return new LLMConnectClient(general, endpoint);
        });

        return services;
    }


    /// <summary>
    /// Adds LLMConnect services to the IServiceCollection with a configuration action for general options only.
    /// </summary>
    /// <param name="services">The IServiceCollection to add services to.</param>
    /// <param name="configureGeneral">The action to configure the general options.</param>
    /// <returns>The IServiceCollection with the added services.</returns>
    public static IServiceCollection AddLLMConnect(
        this IServiceCollection services,
        Action<LLMConnectGeneralOptions> configureGeneral)
    {
        services.Configure(configureGeneral);

        RegisterCoreServices(services);

        services.AddSingleton<ILLMConnectClient>(sp =>
        {
            var general = sp.GetRequiredService<IOptions<LLMConnectGeneralOptions>>().Value;
            // Default endpoint options are created by the client's constructor
            return new LLMConnectClient(general, new LLMConnectEndpointOptions());
        });

        return services;
    }


    // ---------- Core registration (safe, once) ----------

    private static void RegisterCoreServices(IServiceCollection services)
    {
        if (_coreServicesRegistered)
            return;

        lock (_lock)
        {
            if (_coreServicesRegistered)
                return;

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

            _coreServicesRegistered = true;
        }
    }
}