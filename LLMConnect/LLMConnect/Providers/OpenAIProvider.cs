using LLMConnect.Models;
using LLMConnect.Settings;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace LLMConnect;

internal class OpenAIProvider: ProviderBase<OpenAIProvider>, ILLMProvider
{
    private readonly HttpClient _httpClient;
    private readonly LLMConnectEndpointOptions _endpointOpts;

    public OpenAIProvider(HttpClient httpClient, LLMConnectGeneralOptions generalOpts, LLMConnectEndpointOptions endpointOpts) : base(generalOpts)
    {
        _httpClient = httpClient;
        _endpointOpts = endpointOpts;
    }

    public async Task<ChatResponse?> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        _chatRequestValidator.Validate(request, _logger);

        var openAiRequest = request.ToOpenAIRequest(_generalOpts.InternalComputedDefaultModel());
        var json = JsonSerializer.Serialize(openAiRequest, DefaultJsonSerializerOptions);

        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var url = EndpointRegistry.GetEndpointParams(_generalOpts.Provider, QueryType.Chat, false, _logger);
        var response = await _httpClient.PostAsync(url, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
            await LogAndThrow(_generalOpts.Provider, response, cancellationToken);

        return await DeserializeResponseAsync<OpenAIChatResponse, ChatResponse>(
            response,
            _generalOpts.Provider,
            () => _endpointOpts.HasEndpoint && _endpointOpts.HasCustomChatDeserializer,
            async openAIResponseJsonString => await _endpointOpts.ChatResponseDeserializer(openAIResponseJsonString, cancellationToken),
            openAiResponse => openAiResponse?.ToChatResponse(),
            cancellationToken);
    }

    public async IAsyncEnumerable<ChatChunk> StreamAsync(ChatRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _chatRequestValidator.Validate(request, _logger);

        var openAiRequest = request.ToOpenAIRequest(_generalOpts.InternalComputedDefaultModel(request.Model));

        openAiRequest.Stream = true;

        var queryParams = EndpointRegistry.GetEndpointParams(_generalOpts.Provider, QueryType.Chat, true, _logger);
        var url = $"{_httpClient.BaseAddress}{queryParams}";

        var json = JsonSerializer.Serialize(openAiRequest, DefaultJsonSerializerOptions);

        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var messageReq = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = content
        };

        var response = await _httpClient.SendAsync(messageReq, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (!response.IsSuccessStatusCode)
            await LogAndThrow(_generalOpts.Provider, response, cancellationToken);

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        await foreach (var chunk in ReadFromStreamAsync(stream, _generalOpts, _endpointOpts, cancellationToken))
        {
            yield return chunk;
        }
    }

    public async Task<EmbeddingResponse?> GetEmbeddingAsync(EmbeddingRequest request, CancellationToken cancellationToken = default)
    {
        _embeddingRequestValidator?.Validate(request, _logger);

        var openAiRequest = request.ToOpenAIRequest(_generalOpts.InternalComputedDefaultModel(request.Model));
        var json = JsonSerializer.Serialize(openAiRequest, DefaultJsonSerializerOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var url = EndpointRegistry.GetEndpointParams(_generalOpts.Provider, QueryType.Embeddings, false, _logger);
        var response = await _httpClient.PostAsync(url, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
            await LogAndThrow(_generalOpts.Provider, response, cancellationToken);

        return await DeserializeResponseAsync<OpenAIEmbeddingResponse, EmbeddingResponse>(
            response,
            _generalOpts.Provider,
            () => _endpointOpts.HasEndpoint && _endpointOpts.HasCustomEmbeddingDeserializer,
            async openAIResponseJsonString => await _endpointOpts.EmbeddingResponseDeserializer(openAIResponseJsonString, cancellationToken),
            openAiResponse => openAiResponse?.ToEmbeddingResponse(),
            cancellationToken);
    }
}