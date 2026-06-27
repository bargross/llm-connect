using System.Text.Json.Serialization;

namespace LLMConnect;

internal class AnthropicError
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }
}