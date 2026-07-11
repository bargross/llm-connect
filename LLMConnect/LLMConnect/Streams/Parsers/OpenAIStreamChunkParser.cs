using LLMConnect.Models;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LLMConnect;

internal class OpenAIStreamChunkParser : ChunkParserBase<OpenAIStreamChunkParser>, IStreamChunkParser
{
    public OpenAIStreamChunkParser(LLMConnectGeneralOptions options) : base(options) { }

    public ChatChunk? Parse(StreamEvent evt)
    {
        if (string.IsNullOrEmpty(evt.Data) || evt.Data == "[DONE]")
            return null;

        try
        {
            var chunk = JsonSerializer.Deserialize<OpenAIStreamChunk>(evt.Data);
            var choice = chunk?.Choices?.FirstOrDefault();
            if (choice == null)
                return null;

            var result = new ChatChunk();

            if (choice.Delta?.Content is string content && !string.IsNullOrEmpty(content))
                result.Content = content;

            if (choice.Delta?.ToolCalls != null && choice.Delta.ToolCalls.Any())
            {
                result.ToolCalls = choice.Delta.ToolCalls
                    .Select(tc => new ToolCallDelta
                    {
                        Index = tc.Index,
                        Id = tc.Id,
                        Name = tc.Function?.Name,
                        ArgumentsDelta = tc.Function?.Arguments
                    })
                    .ToList();
            }

            if (!string.IsNullOrEmpty(choice.FinishReason))
            {
                result.IsComplete = true;
                result.FinishReason = choice.FinishReason;
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