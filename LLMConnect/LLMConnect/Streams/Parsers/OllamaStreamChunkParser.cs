using LLMConnect.Models;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LLMConnect;

internal class OllamaStreamChunkParser : ChunkParserBase<OllamaStreamChunkParser>, IStreamChunkParser
{
    public OllamaStreamChunkParser(LLMConnectGeneralOptions options) : base(options) { }

    public ChatChunk? Parse(StreamEvent evt)
    {
        if (string.IsNullOrEmpty(evt.Data))
            return null;

        try
        {
            var chunk = JsonSerializer.Deserialize<OllamaChatResponse>(evt.Data);
            if (chunk == null)
                return null;

            var result = new ChatChunk();

            if (chunk.Message?.Content is string content && !string.IsNullOrEmpty(content))
                result.Content = content;

            if (chunk.Message?.ToolCalls != null && chunk.Message.ToolCalls.Any())
            {
                result.ToolCalls = chunk.Message.ToolCalls
                    .Select((tc, idx) => new ToolCallDelta
                    {
                        Index = idx,
                        Id = tc.Function?.Name,
                        Name = tc.Function?.Name,
                        ArgumentsDelta = tc.Function?.Arguments != null
                            ? JsonSerializer.Serialize(tc.Function.Arguments)
                            : null
                    })
                    .ToList();
            }

            if (chunk.Done)
            {
                result.IsComplete = true;
                result.FinishReason = chunk.DoneReason ?? "stop";
            }

            return result;
        }
        catch (JsonException ex)
        {
            _logger?.LogInformation($"Ignoring malformed chunk, reason: {ex.Message}");
            return null;
        }
    }
}