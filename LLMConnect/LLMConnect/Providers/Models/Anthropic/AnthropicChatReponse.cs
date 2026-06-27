using System.Text.Json.Serialization;

namespace LLMConnect;

internal class AnthropicChatResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("stop_reason")]
    public string? StopReason { get; set; }

    [JsonPropertyName("content")]
    public List<AnthropicContentBlock>? Content { get; set; }

    [JsonPropertyName("usage")]
    public AnthropicUsage? Usage { get; set; }
}