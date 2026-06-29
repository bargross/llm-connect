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
                    var status = args.Outcome.Result?.StatusCode;
                    var logMsg = status.HasValue ? status.Value.ToString() : args.Outcome.Exception?.Message ?? "Unknown error";
                    logger?.LogWarning(
                        "Retry {Attempt} after {Delay}ms due to {Status}",
                        args.AttemptNumber,
                        args.RetryDelay.TotalMilliseconds,
                        logMsg);
                    return ValueTask.CompletedTask;
                }
            }).Build();
    }
}