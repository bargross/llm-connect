using System.Text.Json.Serialization;

namespace LLMConnect;

internal class OllamaStreamChunk
{
    [JsonPropertyName("message")]
    public OllamaMessage? Message { get; set; }

    [JsonPropertyName("done")]
    public bool? Done { get; set; }
}