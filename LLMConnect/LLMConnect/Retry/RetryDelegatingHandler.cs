using Polly;

namespace LLMConnect.Internal;

internal class RetryDelegatingHandler : DelegatingHandler
{
    private readonly int _maxRetries;

    public RetryDelegatingHandler(int maxRetries, HttpMessageHandler innerHandler)
        : base(innerHandler)
    {
        _maxRetries = maxRetries;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        return await Policy
            .Handle<HttpRequestException>()
            .OrResult<HttpResponseMessage>(r =>
                r.StatusCode == System.Net.HttpStatusCode.TooManyRequests ||
                (int)r.StatusCode >= 500)
            .WaitAndRetryAsync(
                _maxRetries,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    // Log via the logger if available
                })
            .ExecuteAsync(() => base.SendAsync(request, cancellationToken));
    }
}