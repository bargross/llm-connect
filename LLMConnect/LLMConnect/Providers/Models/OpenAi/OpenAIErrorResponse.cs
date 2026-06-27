using System.Text.Json.Serialization;

namespace LLMConnect;

internal class OpenAIErrorResponse
{
    [JsonPropertyName("error")]
    public OpenAIError? Error { get; set; }
}