using System.Text.Json.Serialization;

namespace LLMConnect;

internal class OllamaToolCall
{
    [JsonPropertyName("function")]
    public OllamaToolCallFunction Function { get; set; } = new();
}