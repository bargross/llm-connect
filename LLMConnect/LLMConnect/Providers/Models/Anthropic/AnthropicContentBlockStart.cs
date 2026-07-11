using System.Text.Json.Serialization;

namespace LLMConnect;

internal class AnthropicContentBlockStart
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("index")]
    public int? Index { get; set; }

    [JsonPropertyName("content_block")]
    public AnthropicContentBlock? ContentBlock { get; set; }
}
