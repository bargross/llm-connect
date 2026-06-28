using LLMConnect.Exceptions;
using LLMConnect.Factories;
using LLMConnect.Models;
using LLMConnect.Providers;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LLMConnect;

internal class OpenAIProvider(HttpClient httpClient, LLMConnectClientOptions options): ProviderBase, ILLMProvider
{
    private readonly ILogger<OpenAIProvider>? _logger = options.LoggerFactory?.CreateLogger<OpenAIProvider>();
    private readonly IChatRequestValidator _validator = ChatRequestValidatorFactory.Create(options.Provider);

    public async Task<ChatResponse?> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        _validator.Validate(request, _logger);

        var openAiRequest = request.ToOpenAIRequest(options.InternalComputedDefaultModel());
        var json = JsonSerializer.Serialize(openAiRequest, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync("", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await ExtractErrorMessage(response, cancellationToken);

            if (_logger != null) _logger.LogError($"OpenAI error: {errorMessage}");

            throw new LLMConnectException("OpenAI", errorMessage);
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

    public async IAsyncEnumerable<ChatChunk> StreamAsync(ChatRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _validator.Validate(request, _logger);

        var openAiRequest = request.ToOpenAIRequest(options.InternalComputedDefaultModel());

        openAiRequest.Stream = true;

        var json = JsonSerializer.Serialize(openAiRequest, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var messageReq = new HttpRequestMessage(HttpMethod.Post, httpClient.BaseAddress)
        {
            Content = content
        };

        var response = await httpClient.SendAsync(messageReq, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await ExtractErrorMessage(response, cancellationToken);
         
            var exception  = new LLMConnectException("OpenAI", errorMessage);

            if (_logger != null) _logger.LogError(exception.Provider, exception.Message, exception);

            throw exception;
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        string? line;
        var count = 1;

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
                _logger?.LogError("OpenAI stream has ended.");

                break; // let the caller handle this on its own
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                if (_logger != null) _logger.LogInformation($"OpenAI stream Line number {count} is empty, ignoring...");

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
                    if (_logger != null) _logger.LogError($"OpenAI stream errored deserializing response due to: {ex.Message}", ex);
                    continue;
                }

                if (chunk?.Choices?.FirstOrDefault()?.Delta?.Content is string contentChunk && !string.IsNullOrEmpty(contentChunk))
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