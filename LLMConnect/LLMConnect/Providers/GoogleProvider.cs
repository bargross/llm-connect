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
        {
            var errorMessage = await ExtractErrorMessage(response, cancellationToken);
            var exception = new LLMConnectException("Google", errorMessage);
            
            if (_logger != null) _logger.LogError(exception.Provider, exception.Message, exception);

            throw exception;
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var googleResponse = JsonSerializer.Deserialize<GoogleChatResponse>(responseJson);

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
        {
            var errorMessage = await ExtractErrorMessage(response, cancellationToken);
            var exception = new LLMConnectException("Google", errorMessage);

            if (_logger != null) _logger.LogError(exception.Provider, exception.Message, exception);

            throw exception;
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

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
                _logger?.LogError("Google stream has ended.");

                break; // let the caller handle this on its own
            }

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

            if (chunk?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text is string contentChunk && !string.IsNullOrEmpty(contentChunk))
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