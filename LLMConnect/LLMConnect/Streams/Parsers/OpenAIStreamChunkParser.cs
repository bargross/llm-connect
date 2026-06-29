using LLMConnect.Models;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LLMConnect;

internal class OpenAIStreamChunkParser : ChunkParserBase<OpenAIStreamChunkParser>, IStreamChunkParser
{
    public OpenAIStreamChunkParser(LLMConnectClientOptions options): base(options) { }

    public ChatChunk? Parse(StreamEvent evt)
    {
        if (string.IsNullOrEmpty(evt.Data) || evt.Data == "[DONE]")
            return null;

        try
        {
            var chunk = JsonSerializer.Deserialize<OpenAIStreamChunk>(evt.Data);
            if (chunk?.Choices?.FirstOrDefault()?.Delta?.Content is string content && !string.IsNullOrEmpty(content))
            {
                return new ChatChunk { Content = content, IsComplete = false };
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