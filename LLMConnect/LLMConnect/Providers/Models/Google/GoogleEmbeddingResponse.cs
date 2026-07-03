using System.Text.Json.Serialization;

internal class GoogleEmbeddingResponse
{
    [JsonPropertyName("embedding")]
    public GoogleEmbeddingData? Embedding { get; set; }
}