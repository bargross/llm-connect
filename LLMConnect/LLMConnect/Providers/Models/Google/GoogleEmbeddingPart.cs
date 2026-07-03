using System.Text.Json.Serialization;

namespace LLMConnect;

internal class GoogleEmbeddingPart
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}