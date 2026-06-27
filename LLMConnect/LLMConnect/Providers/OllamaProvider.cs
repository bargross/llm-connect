using LLMConnect.Exceptions;
using LLMConnect.Models;
using LLMConnect.Settings;
using System.Text;
using System.Text.Json;

namespace LLMConnect;

internal class OllamaProvider(HttpClient httpClient, LLMClientOptions options) : ILLMProvider
{
    private readonly int _maxRetries = options.MaxRetries;

    public async Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        var ollamaRequest = request.ToOllamaRequest();

        var json = JsonSerializer.Serialize(ollamaRequest);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        HttpResponseMessage? response = null;
        int retryCount = 0;

        while (retryCount <= _maxRetries)
        {
            try
            {
                response = await httpClient.PostAsync("", content, cancellationToken);
                if (response.IsSuccessStatusCode)
                    break;

                if ((int)response.StatusCode >= 500 && retryCount < _maxRetries)
                {
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)), cancellationToken);
                    retryCount++;
                    continue;
                }

                var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new LLMConnectException("Ollama", $"HTTP error {response.StatusCode}: {errorJson}");
            }
            catch (HttpRequestException ex) when (retryCount < _maxRetries)
            {
                retryCount++;
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)), cancellationToken);
                continue;
            }
            catch (Exception ex)
            {
                throw new LLMConnectException("Ollama", ex.Message, ex);
            }
        }

        if (response == null)
            throw new LLMConnectException("Ollama", "Failed to get a response after retries");

        if (!response.IsSuccessStatusCode)
        {
            var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new LLMConnectException("Ollama", $"HTTP error {response.StatusCode}: {errorJson}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var ollamaResponse = JsonSerializer.Deserialize<OllamaChatResponse>(responseJson);

        return ollamaResponse.ToChatResponse();
    }

    public async IAsyncEnumerable<ChatChunk> StreamAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        var ollamaRequest = request.ToOllamaRequest();

        ollamaRequest.Stream = true;

        var json = JsonSerializer.Serialize(ollamaRequest);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var retryCount = 0;
        HttpResponseMessage? response = null;

        while (retryCount <= _maxRetries)
        {
            try
            {
                response = await httpClient.PostAsync("", content, cancellationToken);
                if (response.IsSuccessStatusCode)
                    break;

                if ((int)response.StatusCode >= 500 && retryCount < _maxRetries)
                {
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)), cancellationToken);
                    retryCount++;
                    continue;
                }

                var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new LLMConnectException("Ollama", $"HTTP error {response.StatusCode}: {errorJson}");
            }
            catch (HttpRequestException ex) when (retryCount < _maxRetries)
            {
                retryCount++;
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)), cancellationToken);
                continue;
            }
            catch (Exception ex)
            {
                throw new LLMConnectException("Ollama", ex.Message, ex);
            }
        }

        if (response == null || !response.IsSuccessStatusCode)
            throw new LLMConnectException("Ollama", "Failed to get a streaming response");

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