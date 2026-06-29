using LLMConnect;
using Microsoft.Extensions.Logging;
using Polly;

internal class RetryDelegatingHandler : DelegatingHandler
{
    private readonly ResiliencePipeline<HttpResponseMessage> _pipeline;

    public RetryDelegatingHandler(int maxRetries, ILogger? logger = null)
    {
        _pipeline = RetryPipelineFactory.Create(maxRetries, logger);
        InnerHandler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5)
        };
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken) => await _pipeline.ExecuteAsync(async (ct) => await base.SendAsync(request, ct), cancellationToken);
}