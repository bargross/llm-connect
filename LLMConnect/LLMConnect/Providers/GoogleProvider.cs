using LLMConnect.Models;
using LLMConnect.Settings;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace LLMConnect;

internal class GoogleProvider: ProviderBase<GoogleProvider>, ILLMProvider
{
    private readonly HttpClient _httpClient;
    private readonly LLMConnectEndpointOptions _endpointOpts;

    public GoogleProvider(HttpClient httpClient, LLMConnectGeneralOptions generalOpts, LLMConnectEndpointOptions endpointOpts): base(generalOpts)
    {
        _httpClient = httpClient;
        _endpointOpts = endpointOpts;
    }

    public async Task<ChatResponse?> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        _chatRequestValidator.Validate(request, _logger);

        var model = request.Model ?? _generalOpts.InternalComputedDefaultModel(request.Model);
        var url = GetUrl(_endpointOpts, QueryType.Chat, false, model, _httpClient.BaseAddress?.ToString(), _logger);

        var googleRequest = request.ToGoogleRequest();
        var json = JsonSerializer.Serialize(googleRequest, DefaultJsonSerializerOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
            await LogAndThrow(_generalOpts.Provider, response, cancellationToken);

        return await DeserializeResponseAsync<GoogleChatResponse, ChatResponse>(
            response,
            _generalOpts.Provider,
            () => _endpointOpts.HasEndpoint && _endpointOpts.HasCustomChatDeserializer,
            async googleResponseJsonString => await _endpointOpts.ChatResponseDeserializer(googleResponseJsonString, cancellationToken),
            googleResponse => googleResponse?.ToChatResponse(),
            cancellationToken);
    }

    public async IAsyncEnumerable<ChatChunk> StreamAsync(ChatRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _chatRequestValidator.Validate(request, _logger);

        var model = request.Model ?? _generalOpts.InternalComputedDefaultModel(request.Model);

        // google streaming endpoint is different from the normal endpoint, so we need to handle it separately
        var queryParams = EndpointRegistry.GetEndpointParams(_generalOpts.Provider, QueryType.Chat, true, _logger);
        var url = GetUrl(_endpointOpts, QueryType.Chat, true, model, _httpClient.BaseAddress?.ToString(), _logger);

        if (!url.Contains("alt=sse"))
            url += (url.Contains('?') ? "&" : "?") + "alt=sse";

        var googleRequest = request.ToGoogleRequest();

        var json = JsonSerializer.Serialize(googleRequest, DefaultJsonSerializerOptions);
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

        var model = _generalOpts.InternalComputedDefaultModel(request.Model);
        var googleRequest = request.ToGoogleRequest(model);

        var json = JsonSerializer.Serialize(googleRequest, DefaultJsonSerializerOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var url = GetUrl(_endpointOpts, QueryType.Embeddings, false, model, _httpClient.BaseAddress?.ToString(), _logger);
        var response = await _httpClient.PostAsync(url, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
            await LogAndThrow(_generalOpts.Provider, response, cancellationToken);

        return await DeserializeResponseAsync<GoogleEmbeddingResponse, EmbeddingResponse>(
            response,
            _generalOpts.Provider,
            () => _endpointOpts.HasEndpoint && _endpointOpts.HasCustomEmbeddingDeserializer,
            async googleResponseJsonString => await _endpointOpts.EmbeddingResponseDeserializer(googleResponseJsonString, cancellationToken),
            googleResponse => googleResponse?.ToEmbeddingResponse(),
            cancellationToken);
    }
}