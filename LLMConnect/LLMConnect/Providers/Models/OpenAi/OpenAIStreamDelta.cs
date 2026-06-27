using System.Text.Json.Serialization;

namespace LLMConnect;

internal class OpenAIStreamDelta
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }
}