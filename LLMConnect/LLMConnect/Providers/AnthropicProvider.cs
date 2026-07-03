using LLMConnect.Models;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace LLMConnect;

internal class AnthropicProvider(HttpClient httpClient, LLMConnectGeneralOptions generalOpts, LLMConnectEndpointOptions endpointOpts): ProviderBase(generalOpts), ILLMProvider
{
    public async Task<ChatResponse?> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        _chatRequestValidator.Validate(request, _logger);

        var anthropicRequest = request.ToAnthropicRequest(generalOpts.InternalComputedDefaultModel());

        var json = JsonSerializer.Serialize(anthropicRequest, DefaultJsonSerializerOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var queryParams = EndpointRegistry.GetEndpointParams(QueryType.Chat, generalOpts.Provider);
        var response = await httpClient.PostAsync(queryParams, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
            await LogAndThrow(generalOpts.Provider, response, cancellationToken);

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        return !endpointOpts.HasEndpoint ?
            (await GetResponse<AnthropicChatResponse>(response, generalOpts.Provider, cancellationToken))?.ToChatResponse()
            : await DeserializeChatResponseAsync(response, generalOpts.Provider, endpointOpts, cancellationToken);
    }

    public async IAsyncEnumerable<ChatChunk> StreamAsync(ChatRequest request, [EnumeratorCancellation]  CancellationToken cancellationToken = default)
    {
        _chatRequestValidator.Validate(request, _logger);

        var anthropicRequest = request.ToAnthropicRequest(generalOpts.InternalComputedDefaultModel(request.Model));

        anthropicRequest.Stream = true;

        var queryParams = EndpointRegistry.GetEndpointParams(QueryType.Chat, generalOpts.Provider);
        var endpoint = $"{httpClient.BaseAddress}{queryParams}";

        var json = JsonSerializer.Serialize(anthropicRequest, DefaultJsonSerializerOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var messageReq = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = content
        };

        var response = await httpClient.SendAsync(messageReq, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (!response.IsSuccessStatusCode)
            await LogAndThrow(generalOpts.Provider, response, cancellationToken);

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        await foreach (var chunk in ReadFromStreamAsync(stream, generalOpts, endpointOpts, cancellationToken))
        {
            yield return chunk;
        }
    }

    public async Task<EmbeddingResponse?> GetEmbeddingAsync(EmbeddingRequest request, CancellationToken cancellationToken = default)
    {
        var message = "Anthropic does not support embedding generation.";

        _logger?.LogError(message);

        throw new NotSupportedException(message);
    }
    
}