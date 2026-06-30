using LLMConnect.Exceptions;
using LLMConnect.Models;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace LLMConnect;

internal class AnthropicProvider(HttpClient httpClient, LLMConnectClientOptions options): ProviderBase, ILLMProvider
{
    private readonly ILogger<AnthropicProvider>? _logger = options.LoggerFactory?.CreateLogger<AnthropicProvider>();
    private readonly IChatRequestValidator _validator = ChatRequestValidatorFactory
        .Create(options.Provider, options.LoggerFactory?.CreateLogger("ChatRequestValidatorFactory"));

    public async Task<ChatResponse?> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        _validator.Validate(request, _logger);

        var anthropicRequest = request.ToAnthropicRequest(options.InternalComputedDefaultModel());

        var json = JsonSerializer.Serialize(anthropicRequest);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync("", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
            await LogAndThrow(options.Provider, response, _logger, cancellationToken);

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        var anthropicChatResponse = GetResponse<AnthropicChatResponse>(responseJson, _logger, options.Provider);

        return anthropicChatResponse?.ToChatResponse();
    }

    public async IAsyncEnumerable<ChatChunk> StreamAsync(ChatRequest request, [EnumeratorCancellation]  CancellationToken cancellationToken = default)
    {
        _validator.Validate(request, _logger);

        var anthropicRequest = request.ToAnthropicRequest(options.InternalComputedDefaultModel());

        anthropicRequest.Stream = true;

        var json = JsonSerializer.Serialize(anthropicRequest);
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