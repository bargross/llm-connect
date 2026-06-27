using System.Text.Json.Serialization;

namespace LLMConnect;

internal class AnthropicMessageDelta
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "message_delta";

    [JsonPropertyName("delta")]
    public AnthropicMessageDeltaContent? Delta { get; set; }

    [JsonPropertyName("usage")]
    public AnthropicUsage? Usage { get; set; }
}