using System.Text.Json.Serialization;

namespace LLMConnect;

internal class OllamaMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}