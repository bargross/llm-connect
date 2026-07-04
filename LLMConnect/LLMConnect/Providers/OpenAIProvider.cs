using LLMConnect.Models;
using LLMConnect.Settings;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace LLMConnect;

internal class OpenAIProvider(HttpClient httpClient, LLMConnectGeneralOptions generalOpts, LLMConnectEndpointOptions endpointOpts): ProviderBase<OpenAIProvider>(generalOpts), ILLMProvider
{
    public async Task<ChatResponse?> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        _chatRequestValidator.Validate(request, _logger);

        var openAiRequest = request.ToOpenAIRequest(generalOpts.InternalComputedDefaultModel());
        var json = JsonSerializer.Serialize(openAiRequest, DefaultJsonSerializerOptions);

        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var url = EndpointRegistry.GetEndpointParams(QueryType.Chat, generalOpts.Provider);
        var response = await httpClient.PostAsync(url, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
            await LogAndThrow(generalOpts.Provider, response, cancellationToken);

        return await DeserializeResponseAsync<OpenAIChatResponse, ChatResponse>(
            response,
            generalOpts.Provider,
            () => endpointOpts.HasEndpoint && endpointOpts.HasCustomChatDeserializer,
            async openAIResponseJsonString => await endpointOpts.ChatResponseDeserializer(openAIResponseJsonString, cancellationToken),
            openAiResponse => openAiResponse.ToChatResponse(),
            cancellationToken);
    }

    public async IAsyncEnumerable<ChatChunk> StreamAsync(ChatRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _chatRequestValidator.Validate(request, _logger);

        var openAiRequest = request.ToOpenAIRequest(generalOpts.InternalComputedDefaultModel(request.Model));

        openAiRequest.Stream = true;

        var queryParams = EndpointRegistry.GetEndpointParams(QueryType.Chat, generalOpts.Provider);
        var url = $"{httpClient.BaseAddress}{queryParams}";

        var json = JsonSerializer.Serialize(openAiRequest, DefaultJsonSerializerOptions);

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
        _embeddingRequestValidator?.Validate(request, _logger);

        var openAiRequest = request.ToOpenAIRequest(generalOpts.InternalComputedDefaultModel(request.Model));
        var json = JsonSerializer.Serialize(openAiRequest, DefaultJsonSerializerOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var url = EndpointRegistry.GetEndpointParams(QueryType.Embeddings, generalOpts.Provider);
        var response = await httpClient.PostAsync(url, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
            await LogAndThrow(generalOpts.Provider, response, cancellationToken);

        return await DeserializeResponseAsync<OpenAIEmbeddingResponse, EmbeddingResponse>(
            response,
            generalOpts.Provider,
            () => endpointOpts.HasEndpoint && endpointOpts.HasCustomEmbeddingDeserializer,
            async openAIResponseJsonString => await endpointOpts.EmbeddingResponseDeserializer(openAIResponseJsonString, cancellationToken),
            openAiResponse => openAiResponse.ToEmbeddingResponse(),
            cancellationToken);
    }
}