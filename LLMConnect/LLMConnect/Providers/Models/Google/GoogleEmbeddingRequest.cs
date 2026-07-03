using System.Text.Json.Serialization;

namespace LLMConnect;

internal class GoogleEmbeddingRequest
{
    [JsonPropertyName("content")]
    public GoogleEmbeddingContent Content { get; set; } = new();

    [JsonPropertyName("taskType")]
    public string? TaskType { get; set; } // Optional: "RETRIEVAL_DOCUMENT", "RETRIEVAL_QUERY", etc.

    [JsonPropertyName("title")]
    public string? Title { get; set; } // Optional: for document embeddings
}
