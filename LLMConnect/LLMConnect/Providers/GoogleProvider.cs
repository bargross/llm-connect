using LLMConnect.Exceptions;
using LLMConnect.Models;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace LLMConnect;

internal class GoogleProvider(HttpClient httpClient, LLMConnectClientOptions options): ProviderBase, ILLMProvider
{
    private readonly ILogger<GoogleProvider>? _logger = options.LoggerFactory?.CreateLogger<GoogleProvider>();
    private readonly IChatRequestValidator _validator = ChatRequestValidatorFactory.Create(options.Provider);

    public async Task<ChatResponse?> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        _validator.Validate(request, _logger);

        var model = request.Model ?? options.InternalComputedDefaultModel();
        var endpoint = httpClient.BaseAddress?.ToString().Replace("{model}", model);

        var googleRequest = request.ToGoogleRequest();
        var json = JsonSerializer.Serialize(googleRequest);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync(endpoint, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
            await LogAndThrow(options.Provider, response, _logger, cancellationToken);

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        
        var googleResponse = GetResponse<GoogleChatResponse>(responseJson, _logger, options.Provider);

        return googleResponse?.ToChatResponse();
    }

    public async IAsyncEnumerable<ChatChunk> StreamAsync(ChatRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _validator.Validate(request, _logger);

        var model = request.Model ?? options.InternalComputedDefaultModel();
        var baseEndpoint = httpClient.BaseAddress?.ToString();
        var endpoint = baseEndpoint?
            .Replace("{model}", model)
            .Replace("generateContent", "streamGenerateContent");

        if (!endpoint.Contains("alt=sse"))
        {
            endpoint += (endpoint.Contains('?') ? "&" : "?") + "alt=sse";
        }

        var googleRequest = request.ToGoogleRequest();

        var json = JsonSerializer.Serialize(googleRequest);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var messageReq = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = content
        };

        var response = await httpClient.SendAsync(messageReq, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (!response.IsSuccessStatusCode)
            await LogAndThrow(options.Provider, response, _logger, cancellationToken);

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var reader = StreamReaderFactory.Create(options.Provider, options);
        var parser = StreamChunkParserFactory.Create(options.Provider, options);

        await foreach (var evt in reader.ReadEventsAsync(stream, cancellationToken))
        {
            var chunk = parser.Parse(evt);
            if (chunk != null)
                yield return chunk;
        }
    }
}