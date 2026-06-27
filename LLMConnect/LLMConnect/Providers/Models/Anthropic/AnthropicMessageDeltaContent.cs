using System.Text.Json.Serialization;

namespace LLMConnect;

internal class AnthropicMessageDeltaContent
{
    [JsonPropertyName("stop_reason")]
    public string? StopReason { get; set; }

    [JsonPropertyName("stop_sequence")]
    public string? StopSequence { get; set; }
}