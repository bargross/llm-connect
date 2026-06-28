using LLMConnect.Exceptions;
using LLMConnect.Models;
using LLMConnect.Settings;
using System.Text;
using System.Text.Json;

namespace LLMConnect;

internal class GoogleProvider(HttpClient httpClient, LLMClientOptions options) : ILLMProvider
{
    public async Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        var model = request.Model ?? options.DefaultModel ?? "gemini-2.0-flash";
        var endpoint = httpClient.BaseAddress.ToString().Replace("{model}", model);

        var googleRequest = request.ToGoogleRequest();
        var json = JsonSerializer.Serialize(googleRequest);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync(endpoint, content, cancellationToken);

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
        var endpoint = baseEndpoint
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
            throw new LLMConnectException("Google", error?.Error?.Message ?? $"HTTP error: {response.StatusCode}");
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

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