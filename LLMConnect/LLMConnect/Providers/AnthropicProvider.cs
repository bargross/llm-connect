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
    private readonly IChatRequestValidator _validator = ChatRequestValidatorFactory.Create(options.Provider);

    private readonly ILogger<AnthropicProvider>? _logger = options.LoggerFactory?.CreateLogger<AnthropicProvider>();

    public async Task<ChatResponse?> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        _validator.Validate(request, _logger);

        var anthropicRequest = request.ToAnthropicRequest(options.InternalComputedDefaultModel());

        var json = JsonSerializer.Serialize(anthropicRequest);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync("", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await ExtractErrorMessage(response, cancellationToken);

            var exception = new LLMConnectException("Anthropic", errorMessage);

            if (_logger != null) _logger.LogError(exception.Provider, exception.Message, exception);

            throw exception;
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var anthropicResponse = JsonSerializer.Deserialize<AnthropicChatResponse>(responseJson);

        if (anthropicResponse == null)
            throw new LLMConnectException("Anthropic", "Failed to deserialize response");

        return anthropicResponse.ToChatResponse();
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
        {
            var errorMessage = await ExtractErrorMessage(response, cancellationToken);

            var exception = new LLMConnectException("Anthropic", errorMessage);

            if (_logger != null) _logger.LogError(exception.Provider, exception.Message, exception);

            throw exception;
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        string? currentEvent = null;
        string? line;
        var counter = 1;
        var streaming = true;
        while (streaming)
        {
            try
            {

                line = await reader.ReadLineAsync(cancellationToken);
                streaming = line != null;
            }
            catch (OperationCanceledException)
            {
                _logger?.LogError("Anthropic stream has ended.");

                break; // let the caller handle this on its own
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                if (_logger != null) _logger.LogInformation($"Anthropic stream Line {counter} is empty, ignoring...");

                continue;
            }

            if (line.StartsWith("event: ", StringComparison.OrdinalIgnoreCase))
            {
                currentEvent = line.Substring(7).Trim();
                continue;
            }

            if (!line.StartsWith("data: ", StringComparison.OrdinalIgnoreCase))
                continue;

            var data = line.Substring(6).Trim();
            if (data == "[DONE]")
                yield break;

            switch (currentEvent)
            {
                case "content_block_delta":
                    AnthropicContentBlockDelta? delta;
                    try
                    {
                        delta = JsonSerializer.Deserialize<AnthropicContentBlockDelta>(data);
                    }
                    catch (JsonException ex)
                    {
                        if (_logger != null) _logger.LogError($"Anthropic stream error deserializing response due to: {ex.Message}", ex);

                        continue;
                    }

                    if (delta?.Delta?.Text is string textChunk && !string.IsNullOrEmpty(textChunk))
                    {
                        yield return new ChatChunk
                        {
                            Content = textChunk,
                            IsComplete = false
                        };
                    }
                    break;

                case "message_stop":
                    yield break;

                default:
                    break;
            }
        }
    }
}