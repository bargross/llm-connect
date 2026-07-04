using System.Text.Json.Serialization;

namespace LLMConnect;

internal class GoogleEmbeddingResponse
{
    [JsonPropertyName("embedding")]
    public GoogleEmbeddingData? Embedding { get; set; }
}