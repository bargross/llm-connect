using System.Text.Json.Serialization;

namespace LLMConnect;

internal class GoogleEmbeddingData
{
    [JsonPropertyName("values")]
    public float[]? Values { get; set; }
}