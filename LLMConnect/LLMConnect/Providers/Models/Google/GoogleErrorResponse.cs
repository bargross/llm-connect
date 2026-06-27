using System.Text.Json.Serialization;

namespace LLMConnect;

internal class GoogleErrorResponse
{
    [JsonPropertyName("error")]
    public GoogleError? Error { get; set; }
}