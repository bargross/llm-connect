using LLMConnect.Models;
using LLMConnect.Settings;

namespace LLMConnect.Tests.Providers;

internal class TestProvider : ProviderBase<TestProvider>
{
    public TestProvider(LLMConnectGeneralOptions generalOpts) : base(generalOpts) { }

    // Expose protected methods for testing
    public new async Task<string> ExtractErrorMessage(HttpResponseMessage response, CancellationToken cancellationToken)
        => await base.ExtractErrorMessage(response, cancellationToken);

    public new async Task LogAndThrow(ProviderType providerType, HttpResponseMessage response, CancellationToken cancellationToken)
        => await base.LogAndThrow(providerType, response, cancellationToken);

    public new IAsyncEnumerable<ChatChunk> ReadFromStreamAsync(
        Stream stream,
        LLMConnectGeneralOptions generalOpts,
        LLMConnectEndpointOptions endpointOpts,
        CancellationToken cancellationToken)
        => base.ReadFromStreamAsync(stream, generalOpts, endpointOpts, cancellationToken);\

    public List<string> GetRequestedUrls() => _server.RequestedUrls;
        
    public new async Task<TResponse?> DeserializeResponseAsync<TProviderResponse, TResponse>(
        HttpResponseMessage response,
        ProviderType provider,
        Func<bool> customDeserializerEvaluator,
        Func<string, Task<TResponse?>> jsonStringToResponse,
        Func<TProviderResponse?, TResponse?> toChatResponse,
        CancellationToken cancellationToken)
        => await base.DeserializeResponseAsync(
            response,
            provider,
            customDeserializerEvaluator,
            jsonStringToResponse,
            toChatResponse,
            cancellationToken);
}
