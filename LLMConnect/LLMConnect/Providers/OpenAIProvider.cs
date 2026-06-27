using LLMConnect.Exceptions;
using LLMConnect.Models;
using LLMConnect.Settings;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LLMConnect;

internal class OpenAIProvider(HttpClient httpClient, LLMClientOptions options) : ILLMProvider
{
    private readonly int _maxRetries = options.MaxRetries;

    public async Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        var openAiRequest = request.ToOpenAIRequest(options.DefaultModel);

        var json = JsonSerializer.Serialize(openAiRequest, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

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

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests && retryCount < _maxRetries)
                {
                    var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(2);
                    await Task.Delay(retryAfter, cancellationToken);

                    retryCount++;

                    continue;
                }

                var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);
                var error = JsonSerializer.Deserialize<OpenAIErrorResponse>(errorJson);

                throw new LLMConnectException("OpenAI", error?.Error?.Message ?? $"HTTP error: {response.StatusCode}");
            }
            catch (HttpRequestException ex) when (retryCount < _maxRetries)
            {
                retryCount++;

                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)), cancellationToken);

                continue;
            }
            catch (Exception ex)
            {
                throw new LLMConnectException("OpenAI", ex.Message, ex);
            }
        }

        if (response == null)
            throw new LLMConnectException("OpenAI", "Failed to get a response after retries");

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
        var openAiRequest = request.ToOpenAIRequest(options.DefaultModel);
        
        openAiRequest.Stream = true;

        var json = JsonSerializer.Serialize(openAiRequest, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

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

                var error = JsonSerializer.Deserialize<OpenAIErrorResponse>(errorJson);

                throw new LLMConnectException("OpenAI", error?.Error?.Message ?? $"HTTP error: {response.StatusCode}");
            }
            catch (HttpRequestException ex) when (retryCount < _maxRetries)
            {
                retryCount++;

                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)), cancellationToken);

                continue;
            }
            catch (Exception ex)
            {
                throw new LLMConnectException("OpenAI", ex.Message, ex);
            }
        }

        if (response == null)
            throw new LLMConnectException("OpenAI", "Failed to get a response after retries");

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
                {
                    yield break;
                }

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