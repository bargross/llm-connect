using LLMConnect.Exceptions;
using LLMConnect.Models;
using LLMConnect.Settings;
using System.Text;
using System.Text.Json;

namespace LLMConnect;

internal class GoogleProvider(HttpClient httpClient, LLMClientOptions options) : ILLMProvider
{
    public async Task<ChatResponse?> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        var model = request.Model ?? "gemini-2.0-flash";
        var endpoint = httpClient.BaseAddress.ToString().Replace("{model}", model);

        var googleRequest = request.ToGoogleRequest();

        var json = JsonSerializer.Serialize(googleRequest);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        HttpResponseMessage? response = null;
        int retryCount = 0;

        while (retryCount <= options.MaxRetries)
        {
            try
            {
                response = await httpClient.PostAsync(endpoint, content, cancellationToken);
                if (response.IsSuccessStatusCode)
                    break;

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests && retryCount < options.MaxRetries)
                {
                    var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(2);
                    await Task.Delay(retryAfter, cancellationToken);
                    retryCount++;
                    continue;
                }

                var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);
                var error = JsonSerializer.Deserialize<GoogleErrorResponse>(errorJson);

                throw new LLMConnectException("Google", error?.Error?.Message ?? $"HTTP error: {response.StatusCode}");
            }
            catch (HttpRequestException) when (retryCount < options.MaxRetries)
            {
                retryCount++;
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)), cancellationToken);
                continue;
            }
            catch (Exception ex)
            {
                throw new LLMConnectException("Google", ex.Message, ex);
            }
        }

        if (response == null)
            throw new LLMConnectException("Google", "Failed to get a response after retries");

        if (!response.IsSuccessStatusCode)
        {
            var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var error = JsonSerializer.Deserialize<GoogleErrorResponse>(errorJson);
            throw new LLMConnectException("Google", error?.Error?.Message ?? $"HTTP error: {response.StatusCode}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var googleResponse = JsonSerializer.Deserialize<GoogleChatResponse>(responseJson);

        return googleResponse.ToChatResponse();
    }

    public async IAsyncEnumerable<ChatChunk> StreamAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        var model = request.Model ?? options.DefaultModel ?? "gemini-2.0-flash";
        var baseEndpoint = httpClient.BaseAddress.ToString();

        // Replace {model} and switch to the streaming endpoint
        var endpoint = baseEndpoint
            .Replace("{model}", model)
            .Replace("generateContent", "streamGenerateContent");

        var googleRequest = request.ToGoogleRequest(options.DefaultModel);
        var json = JsonSerializer.Serialize(googleRequest);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var retryCount = 0;
        HttpResponseMessage? response = null;

        while (retryCount <= options.MaxRetries)
        {
            try
            {
                response = await httpClient.PostAsync(endpoint, content, cancellationToken);
                if (response.IsSuccessStatusCode)
                    break;

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests && retryCount < options.MaxRetries)
                {
                    var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(2);
                    await Task.Delay(retryAfter, cancellationToken);

                    retryCount++;

                    continue;
                }

                var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);
                var error = JsonSerializer.Deserialize<GoogleErrorResponse>(errorJson);

                throw new LLMConnectException("Google", error?.Error?.Message ?? $"HTTP error: {response.StatusCode}");
            }
            catch (HttpRequestException ex) when (retryCount < options.MaxRetries)
            {
                retryCount++;

                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)), cancellationToken);

                continue;
            }
            catch (Exception ex)
            {
                throw new LLMConnectException("Google", ex.Message, ex);
            }
        }

        if (response == null)
            throw new LLMConnectException("Google", "Failed to get a response after retries");

        if (!response.IsSuccessStatusCode)
        {
            var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var error = JsonSerializer.Deserialize<GoogleErrorResponse>(errorJson);

            throw new LLMConnectException("Google", error?.Error?.Message ?? $"HTTP error: {response.StatusCode}");
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
        {
            if (string.IsNullOrEmpty(line))
                continue;

            // Google SSE sends data lines prefixed with "data: "
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
            catch (JsonException)
            {
                // Ignore malformed chunks
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

            // If usageMetadata is present, we could capture it, but we yield chunks only.
            // The final chunk may have usage; we could yield a final chunk with IsComplete = true,
            // but we'll rely on the [DONE] signal or the end of stream.
        }
    }
}