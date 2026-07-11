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

        var relativePath = GetUrlRelativePath(_endpointOpts, QueryType.Chat, false, null, _logger);
        var response = await _httpClient.PostAsync(relativePath, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
            await LogAndThrow(_generalOpts.Provider, response, cancellationToken);

        return await DeserializeResponseAsync<OllamaChatResponse, ChatResponse>(
            response,
            _generalOpts.Provider,
            ollamaResponse => ollamaResponse?.ToChatResponse(),
            cancellationToken);
    }

    public async IAsyncEnumerable<ChatChunk> StreamAsync(ChatRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _chatRequestValidator.Validate(request, _logger);

        var ollamaRequest = request.ToOllamaRequest(_generalOpts.InternalComputedDefaultModel(request.Model));

        ollamaRequest.Stream = true;

        var relativePath = GetUrlRelativePath(_endpointOpts, QueryType.Chat, true, null, _logger);

        var json = JsonSerializer.Serialize(ollamaRequest, DefaultJsonSerializerOptions);
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

        var ollamaRequest = request.ToOllamaRequest(_generalOpts.InternalComputedDefaultModel(request.Model));
        var json = JsonSerializer.Serialize(ollamaRequest, DefaultJsonSerializerOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var relativePath = GetUrlRelativePath(_endpointOpts, QueryType.Embeddings, false, null, _logger);
        var response = await _httpClient.PostAsync(relativePath, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
            await LogAndThrow(_generalOpts.Provider, response, cancellationToken);

        return await DeserializeResponseAsync<OllamaEmbeddingResponse, EmbeddingResponse>(
            response,
            _generalOpts.Provider,
            ollamaResponse => ollamaResponse?.ToEmbeddingResponse(),
            cancellationToken);
    }
}