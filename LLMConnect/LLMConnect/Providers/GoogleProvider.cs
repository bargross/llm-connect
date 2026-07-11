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
        var relativePath = GetUrlRelativePath(_endpointOpts, QueryType.Chat, false, model, _logger);

        var googleRequest = request.ToGoogleRequest();
        var json = JsonSerializer.Serialize(googleRequest, DefaultJsonSerializerOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(relativePath, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
            await LogAndThrow(_generalOpts.Provider, response, cancellationToken);

        return await DeserializeResponseAsync<GoogleChatResponse, ChatResponse>(
            response,
            _generalOpts.Provider,
            googleResponse => googleResponse?.ToChatResponse(),
            cancellationToken);
    }

    public async IAsyncEnumerable<ChatChunk> StreamAsync(ChatRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _chatRequestValidator.Validate(request, _logger);

        var model = request.Model ?? _generalOpts.InternalComputedDefaultModel(request.Model);

        // google streaming endpoint is different from the normal endpoint, so we need to handle it separately
        var queryParams = EndpointRegistry.GetEndpointParams(_generalOpts.Provider.Value, QueryType.Chat, true, model, _logger);
        var relativePath = GetUrlRelativePath(_endpointOpts, QueryType.Chat, true, model, _logger);

        if (!relativePath.Contains("alt=sse"))
            relativePath += (relativePath.Contains('?') ? "&" : "?") + "alt=sse";

        var googleRequest = request.ToGoogleRequest();

        var json = JsonSerializer.Serialize(googleRequest, DefaultJsonSerializerOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var messageReq = new HttpRequestMessage(HttpMethod.Post, relativePath)
        {
            Content = content
        };

        var response = await _httpClient.SendAsync(messageReq, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (!response.IsSuccessStatusCode)
            await LogAndThrow(_generalOpts.Provider, response, cancellationToken);

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        var reader = StreamReaderFactory.Create(_generalOpts.Provider, _generalOpts);
        var parser = StreamChunkParserFactory.Create(_generalOpts.Provider, _generalOpts);

        await foreach (var evt in reader.ReadEventsAsync(stream, cancellationToken))
        {
            var chunk = parser.Parse(evt);
            if (chunk != null)
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

        var relativePath = GetUrlRelativePath(_endpointOpts, QueryType.Embeddings, false, model, _logger);
        var response = await _httpClient.PostAsync(relativePath, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
            await LogAndThrow(_generalOpts.Provider, response, cancellationToken);

        return await DeserializeResponseAsync<GoogleEmbeddingResponse, EmbeddingResponse>(
            response,
            _generalOpts.Provider,
            googleResponse => googleResponse?.ToEmbeddingResponse(),
            cancellationToken);
    }
}