using System.Text.Json.Serialization;

namespace LLMConnect;

internal class OllamaChatRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public List<OllamaMessage> Messages { get; set; } = new();

    [JsonPropertyName("stream")]
    public bool Stream { get; set; } = false;

    [JsonPropertyName("options")]
    public OllamaOptions? Options { get; set; }
}