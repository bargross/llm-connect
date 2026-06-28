using LLMConnect.Exceptions;
using LLMConnect.Factories;
using LLMConnect.Models;
using LLMConnect.Providers;
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
        {
            var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);

            var errorMessage = await ExtractErrorMessage(response, cancellationToken);
            var exception = new LLMConnectException("Ollama", errorMessage);

            if (_logger != null) _logger.LogError(exception.Provider, exception.Message, exception);

            throw exception;
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var ollamaResponse = JsonSerializer.Deserialize<OllamaChatResponse>(responseJson);

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
        {
            var errorMessage = await ExtractErrorMessage(response, cancellationToken);
         
            var exception = new LLMConnectException("Ollama", errorMessage);

            if (_logger != null) _logger.LogError(exception.Provider, exception.Message, exception);

            throw exception;
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        string? line;
        var counter = 1;
        var streamEnded = false;
        while (streamEnded)
        {
            try
            {

                line = await reader.ReadLineAsync(cancellationToken);
                streamEnded = line != null;

            }
            catch (OperationCanceledException)
            {
                _logger?.LogError("Ollama stream has ended.");

                break; // let the caller handle this on its own
            }

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

            if (chunk?.Message?.Content is string contentChunk && !string.IsNullOrEmpty(contentChunk))
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