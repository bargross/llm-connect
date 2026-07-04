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
        var url = EndpointRegistry.GetEndpointParams(QueryType.Chat, _generalOpts.Provider).Replace("{model}", model);

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
        var queryParams = EndpointRegistry.GetEndpointParams(QueryType.Chat, _generalOpts.Provider);
        var endpoint = _endpointOpts.HasEndpoint ? _endpointOpts.Endpoint 
            : $"{_httpClient.BaseAddress}{queryParams.Replace("{model}", model)}/streamGenerateContent";

        if (!endpoint.Contains("alt=sse"))
            endpoint += (endpoint.Contains('?') ? "&" : "?") + "alt=sse";

        var googleRequest = request.ToGoogleRequest();

        var json = JsonSerializer.Serialize(googleRequest, DefaultJsonSerializerOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var messageReq = new HttpRequestMessage(HttpMethod.Post, endpoint)
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

        var url = EndpointRegistry.GetEndpointParams(QueryType.Embeddings, _generalOpts.Provider).Replace("{model}", model);
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