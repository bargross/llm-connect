using System.Text.Json.Serialization;

namespace LLMConnect;

internal class AnthropicContentBlockDelta
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "content_block_delta";

    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("delta")]
    public AnthropicTextDelta? Delta { get; set; }
}