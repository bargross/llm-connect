using LLMConnect.Exceptions;
using LLMConnect.Models;
using LLMConnect.Settings;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LLMConnect;

internal class OpenAIProvider(HttpClient httpClient, LLMClientOptions options) : ILLMProvider
{
    public async Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        var openAiRequest = request.ToOpenAIRequest(options.InternalComputedDefaultModel);
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

            throw new LLMConnectException("OpenAI", error?.Error?.Message ?? $"HTTP error: {response.StatusCode}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var openAiResponse = JsonSerializer.Deserialize<OpenAIChatResponse>(responseJson);

        if (openAiResponse == null)
            throw new LLMConnectException("OpenAI", "Failed to deserialize response");

        return openAiResponse.ToChatResponse();
    }

    public async IAsyncEnumerable<ChatChunk> StreamAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        var openAiRequest = request.ToOpenAIRequest(options.InternalComputedDefaultModel);

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

            throw new LLMConnectException("OpenAI", error?.Error?.Message ?? $"HTTP error: {response.StatusCode}");
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
        {
            if (string.IsNullOrEmpty(line))
                continue;

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
                catch (JsonException)
                {
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