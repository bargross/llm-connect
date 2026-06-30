using LLMConnect.Models;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LLMConnect;

internal class OllamaStreamChunkParser : ChunkParserBase<OllamaStreamChunkParser>, IStreamChunkParser
{
    public OllamaStreamChunkParser(LLMConnectClientOptions options) : base(options) { }

    public ChatChunk? Parse(StreamEvent evt)
    {
        if (string.IsNullOrEmpty(evt.Data))
            return null;

        try
        {
            var chunk = JsonSerializer.Deserialize<OllamaStreamChunk>(evt.Data);
            if (chunk != null)
            {
                // If the chunk has content, yield it
                if (chunk.Message?.Content is string content && !string.IsNullOrEmpty(content))
                {
                    return new ChatChunk
                    {
                        Content = content,
                        IsComplete = chunk.Done ?? false
                    };
                }
                // If the chunk is marked as done, yield a completion chunk
                if (chunk.Done == true)
                {
                    return new ChatChunk
                    {
                        Content = string.Empty,
                        IsComplete = true
                    };
                }
            }
        }
        catch (JsonException ex)
        {
            _logger?.LogInformation($"Ignoring malformed chunks, reason: {ex.Message}");
        }

        return null;
    }
}