using System.Text.Json.Serialization;

namespace LLMConnect;

internal class GoogleCandidate
{
    [JsonPropertyName("content")]
    public GoogleContent? Content { get; set; }

    [JsonPropertyName("finishReason")]
    public string? FinishReason { get; set; }
}