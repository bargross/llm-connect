using System.Text.Json.Serialization;

namespace LLMConnect;

internal class GoogleChatResponse
{
    [JsonPropertyName("candidates")]
    public List<GoogleCandidate>? Candidates { get; set; }

    [JsonPropertyName("usageMetadata")]
    public GoogleUsageMetadata? UsageMetadata { get; set; }
}