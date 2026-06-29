using LLMConnect.Models;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LLMConnect;

internal class GoogleStreamChunkParser : ChunkParserBase<GoogleStreamChunkParser>, IStreamChunkParser
{
    public GoogleStreamChunkParser(LLMConnectClientOptions options) : base(options) { }

    public ChatChunk? Parse(StreamEvent evt)
    {
        if (string.IsNullOrEmpty(evt.Data) || evt.Data == "[DONE]")
            return null;

        try
        {
            var chunk = JsonSerializer.Deserialize<GoogleChatResponse>(evt.Data);
            if (chunk?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text is string text && !string.IsNullOrEmpty(text))
            {
                return new ChatChunk { Content = text, IsComplete = false };
            }
        }
        catch (JsonException ex)
        {
            _logger?.LogInformation($"Ignoring malformed chunks, reason: {ex.Message}");
            // Ignore malformed chunks
        }

        return null;
    }
}