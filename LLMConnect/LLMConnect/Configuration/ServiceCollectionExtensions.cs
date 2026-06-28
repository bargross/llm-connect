using LLMConnect.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;
using System.Threading.RateLimiting;

namespace LLMConnect.Configuration;

/// <summary>
/// 
/// </summary>
public static class ServiceCollectionExtensions
{

    /// <summary>
    /// 
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configure"></param>
    /// <param name="configureResilience"></param>
    /// <returns></returns>
    public static IServiceCollection AddLLMConnect(
        this IServiceCollection services,
        Action<LLMConnectClientOptions>? configure = null,
        Action<ResiliencePipelineBuilder<HttpResponseMessage>>? configureResilience = null)
    {
        if (configure != null)
            services.Configure(configure);
        else
            services.Configure<LLMConnectClientOptions>(_ => { });

        services.AddHttpClient("LLMConnect")
            .AddResilienceHandler("LLMConnectRetryPipeline", builder =>
            {
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
                            statusCode >= System.Net.HttpStatusCode.InternalServerError ||
                            statusCode == System.Net.HttpStatusCode.TooManyRequests ||
                            args.Outcome.Exception is HttpRequestException);
                    }
                });

                builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
                {
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    FailureRatio = 0.5,
                    MinimumThroughput = 5,
                    ShouldHandle = args => ValueTask.FromResult(true)
                });

                builder.AddRateLimiter(new SlidingWindowRateLimiter(
                    new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromSeconds(60),
                        SegmentsPerWindow = 6
                    }));

                configureResilience?.Invoke(builder);
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