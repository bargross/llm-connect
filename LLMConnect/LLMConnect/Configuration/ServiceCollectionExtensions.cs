using LLMConnect.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;
using Polly.RateLimiting;
using System.Net;
using System.Threading.RateLimiting;

namespace LLMConnect.Configuration;

public static class ServiceCollectionExtensions
{
    // ---------- Unified options (backward compatible) ----------

    public static IServiceCollection AddLLMConnect(
        this IServiceCollection services,
        Action<LLMConnectClientOptions>? configure = null)
    {
        services.Configure(configure ?? (_ => { }));

        // Register the named HttpClient (only once)
        RegisterCoreServices(services);

        services.AddSingleton<ILLMConnectClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<LLMConnectClientOptions>>().Value;
            return new LLMConnectClient(options);
        });

        return services;
    }

    // ---------- Split options (new) ----------

    public static IServiceCollection AddLLMConnect(
        this IServiceCollection services,
        Action<LLMConnectGeneralOptions>? configureGeneral = null,
        Action<LLMConnectEndpointOptions>? configureEndpoint = null)
    {
        services.Configure(configureGeneral ?? (_ => { }));
        services.Configure(configureEndpoint ?? (_ => { }));

        RegisterCoreServices(services);

        services.AddSingleton<ILLMConnectClient>(sp =>
        {
            var general = sp.GetRequiredService<IOptions<LLMConnectGeneralOptions>>().Value;
            var endpoint = sp.GetRequiredService<IOptions<LLMConnectEndpointOptions>>().Value;
            return new LLMConnectClient(general, endpoint);
        });

        return services;
    }

    // Overload: only GeneralOptions
    public static IServiceCollection AddLLMConnect(
        this IServiceCollection services,
        Action<LLMConnectGeneralOptions> configureGeneral)
        => services.AddLLMConnect(configureGeneral, null);

    // ---------- Core registration (safe, once) ----------

    private static readonly object _lock = new object();
    private static bool _coreServicesRegistered = false;

    private static void RegisterCoreServices(IServiceCollection services)
    {
        if (_coreServicesRegistered)
            return;

        lock (_lock)
        {
            if (_coreServicesRegistered)
                return;

            services.AddHttpClient("LLMConnect")
                .AddResilienceHandler("LLMRetryPipeline", builder =>
                {
                    // Retry strategy
                    builder.AddRetry(new HttpRetryStrategyOptions
                    {
                        MaxRetryAttempts = 3,
                        Delay = TimeSpan.FromSeconds(1),
                        BackoffType = DelayBackoffType.Exponential,
                        UseJitter = true,
                        ShouldHandle = args =>
                        {
                            var statusCode = args.Outcome.Result?.StatusCode;
                            return ValueTask.FromResult(
                                statusCode >= HttpStatusCode.InternalServerError ||
                                statusCode == HttpStatusCode.TooManyRequests ||
                                args.Outcome.Exception is HttpRequestException);
                        },
                        OnRetry = args =>
                        {
                            // Logging can be added via the service provider if needed
                            return ValueTask.CompletedTask;
                        }
                    });

                    // Circuit breaker
                    builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
                    {
                        SamplingDuration = TimeSpan.FromSeconds(30),
                        FailureRatio = 0.5,
                        MinimumThroughput = 5,
                        ShouldHandle = args => ValueTask.FromResult(true)
                    });

                    // Rate limiter
                    builder.AddRateLimiter(new SlidingWindowRateLimiter(
                        new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = 100,
                            Window = TimeSpan.FromSeconds(60),
                            SegmentsPerWindow = 6
                        }));
                })
                .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
                {
                    PooledConnectionLifetime = TimeSpan.FromMinutes(5)
                });

            _coreServicesRegistered = true;
        }
    }
}