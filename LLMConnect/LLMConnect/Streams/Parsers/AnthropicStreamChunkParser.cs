using LLMConnect.Models;
using LLMConnect.Settings;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LLMConnect;

internal class AnthropicStreamChunkParser : ChunkParserBase<AnthropicStreamChunkParser>, IStreamChunkParser
{
    private readonly Dictionary<int, AnthropicToolCallState> _toolCallStates = new();

    public AnthropicStreamChunkParser(LLMConnectGeneralOptions options) : base(options) { }

    public ChatChunk? Parse(StreamEvent evt)
    {
        if (string.IsNullOrEmpty(evt.Data) || evt.Data == "[DONE]")
            return null;

        try
        {
            // content_block_start (tool_use)
            if (evt.EventName == "content_block_start")
            {
                var start = JsonSerializer.Deserialize<AnthropicContentBlockStart>(evt.Data);
                if (start?.ContentBlock?.Type == "tool_use")
                {
                    var index = start.Index ?? 0;

                    _toolCallStates[index] = new AnthropicToolCallState
                    {
                        Id = start.ContentBlock.Id,
                        Name = start.ContentBlock.Name,
                        AccumulatedArguments = string.Empty
                    };
                }
                return null;
            }

            // content_block_delta (text or input_json_delta)
            if (evt.EventName == "content_block_delta")
            {
                var delta = JsonSerializer.Deserialize<AnthropicContentBlockDelta>(evt.Data);
                if (delta?.Delta?.Type == "input_json_delta")
                {
                    var index = delta.Index;
                    if (_toolCallStates.TryGetValue(index, out var state))
                    {
                        var partial = delta.Delta.PartialJson ?? string.Empty;
                        state.AccumulatedArguments += partial;

                        // Send only the new partial fragment (not the accumulated)
                        return new ChatChunk
                        {
                            ToolCalls = new List<ToolCallDelta>
                            {
                                new ToolCallDelta
                                {
                                    Index = index,
                                    Id = state.Id,
                                    Name = state.Name,
                                    ArgumentsDelta = partial
                                }
                            }
                        };
                    }
                }
                else if (delta?.Delta?.Text is string text && !string.IsNullOrEmpty(text))
                {
                    return new ChatChunk { Content = text };
                }
                return null;
            }

            // message_delta (carries stop_reason and usage)
            if (evt.EventName == "message_delta")
            {
                var delta = JsonSerializer.Deserialize<AnthropicMessageDelta>(evt.Data);
                if (delta?.Delta?.StopReason != null)
                {
                    return new ChatChunk
                    {
                        IsComplete = true,
                        FinishReason = delta.Delta.StopReason
                    };
                }
                return null;
            }

            // message_stop (end of stream)
            if (evt.EventName == "message_stop")
            {
                _toolCallStates.Clear();
                return new ChatChunk { IsComplete = true };
            }

            return null;
        }
        catch (JsonException ex)
        {
            _logger?.LogInformation($"Ignoring malformed chunk, reason: {ex.Message}");
            return null;
        }
    }
}