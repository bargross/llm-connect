using LLMConnect.Models;
using LLMConnect.Settings;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace LLMConnect;

internal class AnthropicProvider(HttpClient httpClient, LLMConnectGeneralOptions generalOpts, LLMConnectEndpointOptions endpointOpts): ProviderBase<AnthropicProvider>(generalOpts), ILLMProvider
{
    public async Task<ChatResponse?> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        _chatRequestValidator.Validate(request, _logger);

        var anthropicRequest = request.ToAnthropicRequest(generalOpts.InternalComputedDefaultModel());

        var json = JsonSerializer.Serialize(anthropicRequest, DefaultJsonSerializerOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var url = EndpointRegistry.GetEndpointParams(QueryType.Chat, generalOpts.Provider);
        var response = await httpClient.PostAsync(url, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
            await LogAndThrow(generalOpts.Provider, response, cancellationToken);

        return await DeserializeResponseAsync<AnthropicChatResponse, ChatResponse>(
            response,
            generalOpts.Provider,
            () => endpointOpts.HasEndpoint && endpointOpts.HasCustomChatDeserializer,
            async anthropicResponseJsonString => await endpointOpts.ChatResponseDeserializer(anthropicResponseJsonString, cancellationToken),
            anthropicResponse => anthropicResponse.ToChatResponse(),
            cancellationToken);
    }

    public async IAsyncEnumerable<ChatChunk> StreamAsync(ChatRequest request, [EnumeratorCancellation]  CancellationToken cancellationToken = default)
    {
        _chatRequestValidator.Validate(request, _logger);

        var anthropicRequest = request.ToAnthropicRequest(generalOpts.InternalComputedDefaultModel(request.Model));

        anthropicRequest.Stream = true;

        var queryParams = EndpointRegistry.GetEndpointParams(QueryType.Chat, generalOpts.Provider);
        var url = $"{httpClient.BaseAddress}{queryParams}";

        var json = JsonSerializer.Serialize(anthropicRequest, DefaultJsonSerializerOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var messageReq = new HttpRequestMessage(HttpMethod.Post, url)
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
        _embeddingRequestValidator?.Validate(request, _logger); // validation will throw

        throw new NotSupportedException(); // will never reach here
    }
    
}