using System.Text.Json.Serialization;

namespace LLMConnect;

internal class AnthropicErrorResponse
{
    [JsonPropertyName("error")]
    public AnthropicError? Error { get; set; }
}