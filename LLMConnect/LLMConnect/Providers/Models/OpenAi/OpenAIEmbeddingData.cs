using System.Text.Json.Serialization;

internal class OpenAIEmbeddingData
{
    [JsonPropertyName("embedding")]
    public float[]? Embedding { get; set; }
}