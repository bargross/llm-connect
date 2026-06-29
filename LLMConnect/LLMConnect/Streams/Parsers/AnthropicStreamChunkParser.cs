using LLMConnect.Models;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LLMConnect;

internal class AnthropicStreamChunkParser : ChunkParserBase<AnthropicStreamChunkParser>, IStreamChunkParser
{
    public AnthropicStreamChunkParser(LLMConnectClientOptions options) : base(options) { }

    public ChatChunk? Parse(StreamEvent evt)
    {
        if (string.IsNullOrEmpty(evt.Data) || evt.Data == "[DONE]")
            return null;

        if (evt.EventName != "content_block_delta")
            return null;

        try
        {
            var delta = JsonSerializer.Deserialize<AnthropicContentBlockDelta>(evt.Data);
            if (delta?.Delta?.Text is string text && !string.IsNullOrEmpty(text))
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