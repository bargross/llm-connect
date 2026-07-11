using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http;

namespace LLMConnect;

internal class RetryDelegatingHandler : DelegatingHandler
{
    private readonly int _maxRetries;
    private readonly ILogger? _logger;

    public RetryDelegatingHandler(int maxRetries, ILogger? logger = null)
    {
        _maxRetries = maxRetries;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Exception? lastException = null;
        HttpResponseMessage? lastResponse = null;

        if (_maxRetries == 0)
        {
            return await base.SendAsync(request, cancellationToken);
        }

        for (int attempt = 1; attempt <= _maxRetries; attempt++)
        {
            try
            {
                var sendTask = base.SendAsync(request, cancellationToken);
                if (sendTask == null)
                {
                    // Some mocks may return null for additional sequence calls; treat as failure.
                    lastException = new HttpRequestException("Inner handler returned null");
                }
                else
                {
                    var response = await sendTask;
                    lastResponse = response;

                    // If the response is not retryable, return it.
                    if (response.StatusCode != HttpStatusCode.TooManyRequests && (int)response.StatusCode < 500)
                        return response;
                }
            }
            catch (HttpRequestException ex)
            {
                lastException = ex;
            }

            if (attempt == _maxRetries)
                break;

            _logger?.LogWarning("Retry attempt {RetryCount} after {DelayMs}ms.", attempt, TimeSpan.FromSeconds(Math.Pow(2, attempt + 1)).TotalMilliseconds);

            // Backoff delay: use exponential backoff based on retry number (attempt + 1)
            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), cancellationToken);
        }

        if (lastException != null)
            throw lastException;

        // If we exhausted retries due to retryable responses, return the last response.
        return lastResponse!;
    }
}
