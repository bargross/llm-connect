using System.Text.Json.Serialization;

namespace LLMConnect;

internal class OllamaEmbeddingRequest
{
    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("prompt")]
    public string? Prompt { get; set; }

    [JsonPropertyName("options")]
    public Dictionary<string, object>? Options { get; set; } // Optional: provider-specific options
}