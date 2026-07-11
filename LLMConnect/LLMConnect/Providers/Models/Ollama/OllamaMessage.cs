using System.Text.Json.Serialization;

namespace LLMConnect;

internal class OllamaMessage
{
    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("tool_calls")]
    public List<OllamaToolCall>? ToolCalls { get; set; }
}