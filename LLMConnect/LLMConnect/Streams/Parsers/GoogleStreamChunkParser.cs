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

            // Collect all text parts and function call parts
            var textParts = new List<string>();
            var functionParts = new List<GoogleFunctionCall>();

            if (candidate.Content?.Parts != null)
            {
                foreach (var part in candidate.Content.Parts)
                {
                    if (!string.IsNullOrEmpty(part.Text))
                        textParts.Add(part.Text);
                    if (part.FunctionCall != null)
                        functionParts.Add(part.FunctionCall);
                }
            }

            // Combine text parts (if multiple)
            if (textParts.Any())
                result.Content = string.Concat(textParts);

            // Handle all function calls
            if (functionParts.Any())
            {
                result.ToolCalls = functionParts.Select((fc, idx) => new ToolCallDelta
                {
                    Index = idx,
                    Id = fc.Name, // Google uses function name as ID
                    Name = fc.Name,
                    ArgumentsDelta = JsonSerializer.Serialize(fc.Args)
                }).ToList();
            }

            // Finish reason
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