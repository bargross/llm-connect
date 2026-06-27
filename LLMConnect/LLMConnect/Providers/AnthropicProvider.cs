using LLMConnect.Exceptions;
using LLMConnect.Models;
using LLMConnect.Settings;
using System.Text;
using System.Text.Json;

namespace LLMConnect;

internal class AnthropicProvider(HttpClient httpClient, LLMClientOptions options) : ILLMProvider
{
    private readonly int _maxRetries = options.MaxRetries;

    public async Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        var anthropicRequest = request.ToAnthropicRequest();

        var json = JsonSerializer.Serialize(anthropicRequest);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        HttpResponseMessage? response = null;
        var retryCount = 0;

        while (retryCount <= _maxRetries)
        {
            try
            {
                response = await httpClient.PostAsync("", content, cancellationToken);
                if (response.IsSuccessStatusCode)
                    break;

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests && retryCount < _maxRetries)
                {
                    var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(2);
                    await Task.Delay(retryAfter, cancellationToken);
                    retryCount++;
                    continue;
                }

                var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);
                var error = JsonSerializer.Deserialize<AnthropicErrorResponse>(errorJson);
                throw new LLMConnectException("Anthropic", error?.Error?.Message ?? $"HTTP error: {response.StatusCode}");
            }
            catch (HttpRequestException ex) when (retryCount < _maxRetries)
            {
                retryCount++;
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)), cancellationToken);
                continue;
            }
            catch (Exception ex)
            {
                throw new LLMConnectException("Anthropic", ex.Message, ex);
            }
        }

        if (response == null)
            throw new LLMConnectException("Anthropic", "Failed to get a response after retries");

        if (!response.IsSuccessStatusCode)
        {
            var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var error = JsonSerializer.Deserialize<AnthropicErrorResponse>(errorJson);

            throw new LLMConnectException("Anthropic", error?.Error?.Message ?? $"HTTP error: {response.StatusCode}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var anthropicResponse = JsonSerializer.Deserialize<AnthropicChatResponse>(responseJson);

        return anthropicResponse.ToChatResponse();
    }

    public async IAsyncEnumerable<ChatChunk> StreamAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        var anthropicRequest = request.ToAnthropicRequest(options.DefaultModel);

        anthropicRequest.Stream = true;

        var json = JsonSerializer.Serialize(anthropicRequest);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        int retryCount = 0;
        HttpResponseMessage? response = null;

        while (retryCount <= _maxRetries)
        {
            try
            {
                response = await httpClient.PostAsync("", content, cancellationToken);
                if (response.IsSuccessStatusCode)
                    break;

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests && retryCount < _maxRetries)
                {
                    var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(2);
                    await Task.Delay(retryAfter, cancellationToken);
                    retryCount++;
                    continue;
                }

                var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);
                var error = JsonSerializer.Deserialize<AnthropicErrorResponse>(errorJson);

                throw new LLMConnectException("Anthropic", error?.Error?.Message ?? $"HTTP error: {response.StatusCode}");
            }
            catch (HttpRequestException ex) when (retryCount < _maxRetries)
            {
                retryCount++;
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)), cancellationToken);
                continue;
            }
            catch (Exception ex)
            {
                throw new LLMConnectException("Anthropic", ex.Message, ex);
            }
        }

        if (response == null)
            throw new LLMConnectException("Anthropic", "Failed to get a response after retries");

        if (!response.IsSuccessStatusCode)
        {
            var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var error = JsonSerializer.Deserialize<AnthropicErrorResponse>(errorJson);
            throw new LLMConnectException("Anthropic", error?.Error?.Message ?? $"HTTP error: {response.StatusCode}");
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        string? currentEvent = null;
        string? line;

        while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
        {
            if (string.IsNullOrEmpty(line))
                continue;

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
                    catch (JsonException)
                    {
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