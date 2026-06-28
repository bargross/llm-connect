using LLMConnect.Exceptions;
using LLMConnect.Models;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace LLMConnect;

internal class OllamaProvider(HttpClient httpClient, LLMConnectClientOptions options) : ILLMProvider
{
    private readonly ILogger<OllamaProvider>? _logger = options.LoggerFactory?.CreateLogger<OllamaProvider>();

    public async Task<ChatResponse?> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        var ollamaRequest = request.ToOllamaRequest(options.InternalComputedDefaultModel());
        var json = JsonSerializer.Serialize(ollamaRequest);

        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync("", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);

            var exception = new LLMConnectException("Ollama", $"HTTP error {response.StatusCode}: {errorJson}");

            if (_logger != null) _logger.LogError(exception.Provider, exception.Message, exception);

            throw exception;
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var ollamaResponse = JsonSerializer.Deserialize<OllamaChatResponse>(responseJson);

        return ollamaResponse?.ToChatResponse();
    }

    public async IAsyncEnumerable<ChatChunk> StreamAsync(ChatRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var ollamaRequest = request.ToOllamaRequest(options.InternalComputedDefaultModel());

        ollamaRequest.Stream = true;

        var json = JsonSerializer.Serialize(ollamaRequest);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync("", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var exception = new LLMConnectException("Ollama", $"HTTP error {response.StatusCode}: {errorJson}");

            if (_logger != null) _logger.LogError(exception.Provider, exception.Message, exception);

            throw exception;
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        string? line;
        var counter = 1;
        while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                if (_logger != null) _logger.LogInformation($"Ollama stream Line {counter} is empty, ignoring....");

                continue;
            }
            
            counter++;

            OllamaStreamChunk? chunk = null;
            try
            {
                chunk = JsonSerializer.Deserialize<OllamaStreamChunk>(line);
            }
            catch (JsonException ex)
            {
                if (_logger != null) _logger.LogError($"Ollama stream errored deserializing response due to: {ex.Message}", ex);

                continue;
            }

            if (chunk?.Message?.Content is string contentChunk && !string.IsNullOrWhiteSpace(contentChunk))
            {
                yield return new ChatChunk
                {
                    Content = contentChunk,
                    IsComplete = chunk.Done ?? false
                };
            }

            if (chunk?.Done == true)
                yield break;
        }
    }
}