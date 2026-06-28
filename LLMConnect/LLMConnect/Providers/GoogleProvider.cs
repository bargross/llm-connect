using LLMConnect.Exceptions;
using LLMConnect.Models;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace LLMConnect;

internal class GoogleProvider(HttpClient httpClient, LLMConnectClientOptions options) : ILLMProvider
{
    private readonly ILogger<GoogleProvider>? _logger = options.LoggerFactory?.CreateLogger<GoogleProvider>();

    public async Task<ChatResponse?> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        var model = request.Model ?? options.InternalComputedDefaultModel();
        var endpoint = httpClient.BaseAddress?.ToString().Replace("{model}", model);

        var googleRequest = request.ToGoogleRequest();
        var json = JsonSerializer.Serialize(googleRequest);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync(endpoint, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var error = JsonSerializer.Deserialize<GoogleErrorResponse>(errorJson);

            var exception = new LLMConnectException("Google", error?.Error?.Message ?? $"HTTP error: {response.StatusCode}");
            
            if (_logger != null) _logger.LogError(exception.Provider, exception.Message, exception);

            throw exception;
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var googleResponse = JsonSerializer.Deserialize<GoogleChatResponse>(responseJson);

        return googleResponse?.ToChatResponse();
    }

    public async IAsyncEnumerable<ChatChunk> StreamAsync(ChatRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var model = request.Model ?? options.InternalComputedDefaultModel();
        var baseEndpoint = httpClient.BaseAddress?.ToString();
        var endpoint = baseEndpoint?
            .Replace("{model}", model)
            .Replace("generateContent", "streamGenerateContent");

        var googleRequest = request.ToGoogleRequest();

        var json = JsonSerializer.Serialize(googleRequest);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync(endpoint, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var error = JsonSerializer.Deserialize<GoogleErrorResponse>(errorJson);

            var exception = new LLMConnectException("Google", error?.Error?.Message ?? $"HTTP error: {response.StatusCode}");

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
                if (_logger != null) _logger.LogInformation($"Google stream Line {counter} is empty, ignoring...");

                continue;
            }

            counter++;

            if (!line.StartsWith("data: ", StringComparison.OrdinalIgnoreCase))
                continue;

            var data = line.Substring(6);
            if (data == "[DONE]")
                yield break;

            GoogleChatResponse? chunk = null;
            try
            {
                chunk = JsonSerializer.Deserialize<GoogleChatResponse>(data);
            }
            catch (JsonException ex)
            {
                if (_logger != null) _logger.LogError($"Google stream error deserializing response due to: {ex.Message}", ex);

                continue;
            }

            if (chunk?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text is string contentChunk && !string.IsNullOrWhiteSpace(contentChunk))
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