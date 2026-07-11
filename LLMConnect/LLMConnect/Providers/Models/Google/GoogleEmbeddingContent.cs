using System.Text.Json.Serialization;

namespace LLMConnect;

internal class GoogleEmbeddingContent
{
    [JsonPropertyName("parts")]
    public List<GoogleEmbeddingPart> Parts { get; set; } = new();

    [JsonPropertyName("role")]
    public string? Role { get; set; } // Optional: "user" or "model"
}