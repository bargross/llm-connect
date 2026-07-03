using System.Text.Json.Serialization;

namespace LLMConnect;

internal class OllamaEmbeddingRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("prompt")]
    public string Prompt { get; set; } = string.Empty;

    [JsonPropertyName("options")]
    public Dictionary<string, object>? Options { get; set; } // Optional: provider-specific options
}