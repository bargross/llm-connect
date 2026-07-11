using System.Text.Json.Serialization;

namespace LLMConnect;

internal class OpenAIStreamDelta
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("tool_calls")]
    public List<OpenAIStreamToolCall>? ToolCalls { get; set; }
}