using System.Text.Json.Serialization;

namespace LLMConnect;

internal class AnthropicTextDelta
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "text_delta";

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}