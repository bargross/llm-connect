using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System.Net;

namespace LLMConnect;

internal static class RetryPipelineFactory
{
    public static ResiliencePipeline<HttpResponseMessage> Create(int maxRetries, ILogger? logger)
    {
        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = maxRetries,
                DelayGenerator = args =>
                {
                    // Check for Retry-After header
                    var response = args.Outcome.Result;
                    if (response?.Headers?.RetryAfter?.Delta is TimeSpan retryAfter && retryAfter > TimeSpan.Zero)
                    {
                        logger?.LogDebug("Using Retry-After header: {RetryAfter} seconds", retryAfter.TotalSeconds);

                        return ValueTask.FromResult<TimeSpan?>(retryAfter);
                    }

                    // Fallback: exponential backoff with jitter
                    var baseDelay = TimeSpan.FromSeconds(1);
                    var delay = TimeSpan.FromMilliseconds(
                        Math.Pow(2, args.AttemptNumber) * baseDelay.TotalMilliseconds
                    );

                    // Add jitter (±20%)
                    var jitter = Random.Shared.NextDouble() * 0.4 - 0.2; // -20% to +20%
                    var jitteredDelay = delay * (1 + jitter);
                    jitteredDelay = TimeSpan.FromMilliseconds(Math.Max(0, jitteredDelay.TotalMilliseconds));

                    return ValueTask.FromResult<TimeSpan?>(jitteredDelay);
                },
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
                    var status = args.Outcome.Result?.StatusCode;
                    var logMsg = status.HasValue ? status.Value.ToString() : args.Outcome.Exception?.Message ?? "Unknown error";
                    
                    logger?.LogWarning(
                        "Retry {Attempt} after {Delay}ms due to {Status}",
                        args.AttemptNumber,
                        args.RetryDelay.TotalMilliseconds,
                        logMsg);

                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }
}