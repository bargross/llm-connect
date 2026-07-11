using System.Text.Json.Serialization;

internal class OllamaEmbeddingResponse
{
    [JsonPropertyName("embedding")]
    public float[]? Embedding { get; set; }
}