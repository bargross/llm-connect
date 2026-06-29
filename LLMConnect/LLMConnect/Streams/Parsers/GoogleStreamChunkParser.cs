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
        if (string.IsNullOrEmpty(evt.Data))
            return null;

        try
        {
            var chunk = JsonSerializer.Deserialize<GoogleChatResponse>(evt.Data);
            var candidate = chunk?.Candidates?.FirstOrDefault();
            var text = candidate?.Content?.Parts?.FirstOrDefault()?.Text;

            if (!string.IsNullOrEmpty(text))
            {
                return new ChatChunk
                {
                    Content = text,
                    IsComplete = false,
                    FinishReason = null
                };
            }

            // If this chunk contains a finishReason, yield the final chunk
            if (candidate?.FinishReason != null)
            {
                return new ChatChunk
                {
                    Content = string.Empty,
                    IsComplete = true,
                    FinishReason = candidate.FinishReason
                };
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