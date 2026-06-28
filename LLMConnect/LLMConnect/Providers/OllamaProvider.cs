using LLMConnect.Exceptions;
using LLMConnect.Models;
using LLMConnect.Settings;
using System.Text;
using System.Text.Json;

namespace LLMConnect;

internal class OllamaProvider(HttpClient httpClient, LLMConnectClientOptions options) : ILLMProvider
{
    public async Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        var ollamaRequest = request.ToOllamaRequest(options.InternalComputedDefaultModel());
        var json = JsonSerializer.Serialize(ollamaRequest);

        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync("", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new LLMConnectException("Ollama", $"HTTP error {response.StatusCode}: {errorJson}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var ollamaResponse = JsonSerializer.Deserialize<OllamaChatResponse>(responseJson);

        return ollamaResponse?.ToChatResponse();
    }

    public async IAsyncEnumerable<ChatChunk> StreamAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        var ollamaRequest = request.ToOllamaRequest(options.InternalComputedDefaultModel());

        ollamaRequest.Stream = true;

        var json = JsonSerializer.Serialize(ollamaRequest);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync("", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new LLMConnectException("Ollama", $"HTTP error {response.StatusCode}: {errorJson}");
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
        {
            if (string.IsNullOrEmpty(line))
                continue;

            OllamaStreamChunk? chunk = null;
            try
            {
                chunk = JsonSerializer.Deserialize<OllamaStreamChunk>(line);
            }
            catch (JsonException)
            {
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