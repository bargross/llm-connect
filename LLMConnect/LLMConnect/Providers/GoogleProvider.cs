using LLMConnect.Models;
using LLMConnect.Settings;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace LLMConnect;

internal class GoogleProvider(HttpClient httpClient, LLMConnectGeneralOptions generalOpts, LLMConnectEndpointOptions endpointOpts) : ProviderBase<GoogleProvider>(generalOpts), ILLMProvider
{
    public async Task<ChatResponse?> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        _chatRequestValidator.Validate(request, _logger);

        var model = request.Model ?? generalOpts.InternalComputedDefaultModel(request.Model);
        var url = EndpointRegistry.GetEndpointParams(QueryType.Chat, generalOpts.Provider).Replace("{model}", model);;

        var googleRequest = request.ToGoogleRequest();
        var json = JsonSerializer.Serialize(googleRequest, DefaultJsonSerializerOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync(url, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
            await LogAndThrow(generalOpts.Provider, response, cancellationToken);

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        return await DeserializeResponseAsync<GoogleChatResponse, ChatResponse>(
            response,
            generalOpts.Provider,
            () => endpointOpts.HasEndpoint && endpointOpts.HasCustomChatDeserializer,
            async googleResponseJsonString => await endpointOpts.ChatResponseDeserializer(googleResponseJsonString, cancellationToken),
            googleResponse => googleResponse.ToChatResponse(),
            cancellationToken);
    }

    public async IAsyncEnumerable<ChatChunk> StreamAsync(ChatRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _chatRequestValidator.Validate(request, _logger);

        var model = request.Model ?? generalOpts.InternalComputedDefaultModel(request.Model);

        // google streaming endpoint is different from the normal endpoint, so we need to handle it separately
        var queryParams = EndpointRegistry.GetEndpointParams(QueryType.Chat, generalOpts.Provider);
        var endpoint = endpointOpts.HasEndpoint ? endpointOpts.Endpoint 
            : $"{httpClient.BaseAddress}{queryParams.Replace("{model}", model)}/streamGenerateContent";

        if (!endpoint.Contains("alt=sse"))
            endpoint += (endpoint.Contains('?') ? "&" : "?") + "alt=sse";

        var googleRequest = request.ToGoogleRequest();

        var json = JsonSerializer.Serialize(googleRequest, DefaultJsonSerializerOptions);
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
        _embeddingRequestValidator?.Validate(request, _logger);

        var model = generalOpts.InternalComputedDefaultModel(request.Model);
        var googleRequest = request.ToGoogleRequest(model);

        var json = JsonSerializer.Serialize(googleRequest, DefaultJsonSerializerOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var queryParams = EndpointRegistry.GetEndpointParams(QueryType.Embeddings, generalOpts.Provider);
        var response = await httpClient.PostAsync(queryParams, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
            await LogAndThrow(generalOpts.Provider, response, cancellationToken);

        return await DeserializeResponseAsync<GoogleEmbeddingResponse, EmbeddingResponse>(
            response,
            generalOpts.Provider,
            () => endpointOpts.HasEndpoint && endpointOpts.HasCustomEmbeddingDeserializer,
            async googleResponseJsonString => await endpointOpts.EmbeddingResponseDeserializer(googleResponseJsonString, cancellationToken),
            googleResponse => googleResponse.ToEmbeddingResponse(),
            cancellationToken);
    }
}