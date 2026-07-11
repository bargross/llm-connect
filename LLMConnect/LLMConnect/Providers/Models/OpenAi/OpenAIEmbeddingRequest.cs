using System.Text.Json.Serialization;

namespace LLMConnect;

internal class OpenAIEmbeddingRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = "text-embedding-3-small";

    [JsonPropertyName("input")]
    public string Input { get; set; } = string.Empty;

    [JsonPropertyName("encoding_format")]
    public string? EncodingFormat { get; set; } // "float" or "base64"

    [JsonPropertyName("dimensions")]
    public int? Dimensions { get; set; } // Optional: for models that support it

    [JsonPropertyName("user")]
    public string? User { get; set; } // Optional: for abuse monitoring
}