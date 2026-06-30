using LLMConnect.Exceptions;
using LLMConnect.Models;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace LLMConnect;

internal class OllamaProvider(HttpClient httpClient, LLMConnectClientOptions options): ProviderBase, ILLMProvider
{
    private readonly ILogger<OllamaProvider>? _logger = options.LoggerFactory?.CreateLogger<OllamaProvider>();
    private readonly IChatRequestValidator _validator = ChatRequestValidatorFactory.Create(options.Provider);

    public async Task<ChatResponse?> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        _validator.Validate(request, _logger);

        var ollamaRequest = request.ToOllamaRequest(options.InternalComputedDefaultModel());
        var json = JsonSerializer.Serialize(ollamaRequest);

        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync("", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
            await LogAndThrow(options.Provider, response, _logger, cancellationToken);

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        var ollamaResponse = GetResponse<OllamaChatResponse>(responseJson, _logger, options.Provider);

        return ollamaResponse?.ToChatResponse();
    }

    public async IAsyncEnumerable<ChatChunk> StreamAsync(ChatRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _validator.Validate(request, _logger);

        var ollamaRequest = request.ToOllamaRequest(options.InternalComputedDefaultModel());

        ollamaRequest.Stream = true;

        var json = JsonSerializer.Serialize(ollamaRequest);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var messageReq = new HttpRequestMessage(HttpMethod.Post, httpClient.BaseAddress)
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