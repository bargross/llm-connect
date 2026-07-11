using LLMConnect.Models;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LLMConnect;

internal class GoogleStreamChunkParser : ChunkParserBase<GoogleStreamChunkParser>, IStreamChunkParser
{
    public GoogleStreamChunkParser(LLMConnectGeneralOptions options) : base(options) { }

    public ChatChunk? Parse(StreamEvent evt)
    {
        if (string.IsNullOrEmpty(evt.Data))
            return null;

        try
        {
            var chunk = JsonSerializer.Deserialize<GoogleChatResponse>(evt.Data);
            var candidate = chunk?.Candidates?.FirstOrDefault();
            if (candidate == null)
                return null;

            var result = new ChatChunk();

            var textPart = candidate.Content?.Parts?.FirstOrDefault(p => p.Text != null);
            if (textPart?.Text is string text && !string.IsNullOrEmpty(text))
                result.Content = text;

            var functionPart = candidate.Content?.Parts?.FirstOrDefault(p => p.FunctionCall != null);
            if (functionPart?.FunctionCall != null)
            {
                var fc = functionPart.FunctionCall;
                var argsJson = JsonSerializer.Serialize(fc.Args);
                result.ToolCalls = new List<ToolCallDelta>
                {
                    new ToolCallDelta
                    {
                        Index = 0,
                        Id = fc.Name,
                        Name = fc.Name,
                        ArgumentsDelta = argsJson
                    }
                };
            }

            if (!string.IsNullOrEmpty(candidate.FinishReason))
            {
                result.IsComplete = true;
                result.FinishReason = candidate.FinishReason;
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