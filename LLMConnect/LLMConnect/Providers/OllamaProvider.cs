using LLMConnect.Models;
using LLMConnect.Settings;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace LLMConnect;

internal class OllamaProvider: ProviderBase<OllamaProvider>, ILLMProvider
{
    private readonly HttpClient _httpClient;
    private readonly LLMConnectEndpointOptions _endpointOpts;

    public OllamaProvider(HttpClient httpClient, LLMConnectGeneralOptions generalOpts, LLMConnectEndpointOptions endpointOpts) : base(generalOpts)
    {
        _httpClient = httpClient;
        _endpointOpts = endpointOpts;
    }

    public async Task<ChatResponse?> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        _chatRequestValidator.Validate(request, _logger);

        var ollamaRequest = request.ToOllamaRequest(_generalOpts.InternalComputedDefaultModel(request.Model));
        var json = JsonSerializer.Serialize(ollamaRequest, DefaultJsonSerializerOptions);

        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var url = GetUrl(_endpointOpts, QueryType.Chat, false, null, _httpClient.BaseAddress?.ToString(), _logger);
        var response = await _httpClient.PostAsync(url, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
            await LogAndThrow(_generalOpts.Provider, response, cancellationToken);

        return await DeserializeResponseAsync<OllamaChatResponse, ChatResponse>(
            response,
            _generalOpts.Provider,
            () => _endpointOpts.HasEndpoint && _endpointOpts.HasCustomChatDeserializer,
            async ollamaResponseJsonString => await _endpointOpts.ChatResponseDeserializer(ollamaResponseJsonString, cancellationToken),
            ollamaResponse => ollamaResponse?.ToChatResponse(),
            cancellationToken);
    }

    public async IAsyncEnumerable<ChatChunk> StreamAsync(ChatRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _chatRequestValidator.Validate(request, _logger);

        var ollamaRequest = request.ToOllamaRequest(_generalOpts.InternalComputedDefaultModel(request.Model));

        ollamaRequest.Stream = true;

        var url = GetUrl(_endpointOpts, QueryType.Chat, true, null, _httpClient.BaseAddress?.ToString(), _logger);

        var json = JsonSerializer.Serialize(ollamaRequest, DefaultJsonSerializerOptions);
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

        var ollamaRequest = request.ToOllamaRequest(_generalOpts.InternalComputedDefaultModel(request.Model));
        var json = JsonSerializer.Serialize(ollamaRequest, DefaultJsonSerializerOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var url = GetUrl(_endpointOpts, QueryType.Embeddings, false, null, _httpClient.BaseAddress?.ToString(), _logger);
        var response = await _httpClient.PostAsync(url, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
            await LogAndThrow(_generalOpts.Provider, response, cancellationToken);

        return await DeserializeResponseAsync<OllamaEmbeddingResponse, EmbeddingResponse>(
            response,
            _generalOpts.Provider,
            () => _endpointOpts.HasEndpoint && _endpointOpts.HasCustomEmbeddingDeserializer,
            async ollamaResponseJsonString => await _endpointOpts.EmbeddingResponseDeserializer(ollamaResponseJsonString, cancellationToken),
            ollamaResponse => ollamaResponse?.ToEmbeddingResponse(),
            cancellationToken);
    }
}