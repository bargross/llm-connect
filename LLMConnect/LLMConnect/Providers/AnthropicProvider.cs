using LLMConnect.Models;
using LLMConnect.Settings;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace LLMConnect;

internal class AnthropicProvider: ProviderBase<AnthropicProvider>, ILLMProvider
{
    private readonly HttpClient _httpClient;
    private readonly LLMConnectEndpointOptions _endpointOpts;

    public AnthropicProvider(HttpClient httpClient, LLMConnectGeneralOptions generalOpts, LLMConnectEndpointOptions endpointOpts) : base(generalOpts)
    {
        _httpClient = httpClient;
        _endpointOpts = endpointOpts;
    }

    public async Task<ChatResponse?> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        _chatRequestValidator.Validate(request, _logger);

        var anthropicRequest = request.ToAnthropicRequest(_generalOpts.InternalComputedDefaultModel());

        var json = JsonSerializer.Serialize(anthropicRequest, DefaultJsonSerializerOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var url = EndpointRegistry.GetEndpointParams(QueryType.Chat, _generalOpts.Provider);
        var response = await _httpClient.PostAsync(url, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
            await LogAndThrow(_generalOpts.Provider, response, cancellationToken);

        return await DeserializeResponseAsync<AnthropicChatResponse, ChatResponse>(
            response,
            _generalOpts.Provider,
            () => _endpointOpts.HasEndpoint && _endpointOpts.HasCustomChatDeserializer,
            async anthropicResponseJsonString => await _endpointOpts.ChatResponseDeserializer(anthropicResponseJsonString, cancellationToken),
            anthropicResponse => anthropicResponse?.ToChatResponse(),
            cancellationToken);
    }

    public async IAsyncEnumerable<ChatChunk> StreamAsync(ChatRequest request, [EnumeratorCancellation]  CancellationToken cancellationToken = default)
    {
        _chatRequestValidator.Validate(request, _logger);

        var anthropicRequest = request.ToAnthropicRequest(_generalOpts.InternalComputedDefaultModel(request.Model));

        anthropicRequest.Stream = true;

        var queryParams = EndpointRegistry.GetEndpointParams(QueryType.Chat, _generalOpts.Provider);
        var url = $"{_httpClient.BaseAddress}{queryParams}";

        var json = JsonSerializer.Serialize(anthropicRequest, DefaultJsonSerializerOptions);
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
        _embeddingRequestValidator?.Validate(request, _logger); // validation will throw

        throw new NotSupportedException(); // will never reach here
    }
    
}