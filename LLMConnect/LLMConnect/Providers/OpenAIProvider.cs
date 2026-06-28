using LLMConnect.Exceptions;
using LLMConnect.Models;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LLMConnect;

internal class OpenAIProvider(HttpClient httpClient, LLMConnectClientOptions options) : ILLMProvider
{
    private readonly ILogger<OpenAIProvider>? _logger = options.LoggerFactory?.CreateLogger<OpenAIProvider>();

    public async Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        var openAiRequest = request.ToOpenAIRequest(options.InternalComputedDefaultModel());
        var json = JsonSerializer.Serialize(openAiRequest, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync("", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var error = JsonSerializer.Deserialize<OpenAIErrorResponse>(errorJson);

            if (_logger != null) _logger.LogError(error.Error.Message, error);

            throw new LLMConnectException("OpenAI", error?.Error?.Message ?? $"HTTP error: {response.StatusCode}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var openAiResponse = JsonSerializer.Deserialize<OpenAIChatResponse>(responseJson);

        if (openAiResponse == null)
        {
            var exception = new LLMConnectException("OpenAI", "Failed to deserialize response");

            if (_logger != null) _logger.LogError(exception.Provider, exception.Message, exception);

            throw exception;
        }

        return openAiResponse.ToChatResponse();
    }

    public async IAsyncEnumerable<ChatChunk> StreamAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        var openAiRequest = request.ToOpenAIRequest(options.InternalComputedDefaultModel());

        openAiRequest.Stream = true;

        var json = JsonSerializer.Serialize(openAiRequest, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync("", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var error = JsonSerializer.Deserialize<OpenAIErrorResponse>(errorJson);

            var exception  = new LLMConnectException("OpenAI", error?.Error?.Message ?? $"HTTP error: {response.StatusCode}");

            if (_logger != null) _logger.LogError(exception.Provider, exception.Message, exception);

            throw exception;
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        string? line;
        var count = 1;
        while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                if (_logger != null) _logger.LogInformation($"Line number {count} is empty, ignoring...");

                continue;
            }

            count++; 

            if (line.StartsWith("data: ", StringComparison.OrdinalIgnoreCase))
            {
                var data = line.Substring(6);
                if (data == "[DONE]")
                    yield break;

                OpenAiStreamChunk? chunk = null;
                try
                {
                    chunk = JsonSerializer.Deserialize<OpenAiStreamChunk>(data);
                }
                catch (JsonException ex)
                {
                    if (_logger != null) _logger.LogError("OpenAI", $"Error deserializing response due to: {ex.Message}", ex);
                    continue;
                }

                if (chunk?.Choices?.FirstOrDefault()?.Delta?.Content is string contentChunk && !string.IsNullOrWhiteSpace(contentChunk))
                {
                    yield return new ChatChunk
                    {
                        Content = contentChunk,
                        IsComplete = false
                    };
                }
            }
        }
    }
}