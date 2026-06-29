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
            if (chunk?.Message?.Content is string content && !string.IsNullOrEmpty(content))
            {
                return new ChatChunk
                {
                    Content = content,
                    IsComplete = chunk.Done ?? false
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