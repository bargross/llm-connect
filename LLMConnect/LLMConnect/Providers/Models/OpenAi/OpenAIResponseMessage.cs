using System.Text.Json.Serialization;

namespace LLMConnect;

internal class OpenAIResponseMessage
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }
}