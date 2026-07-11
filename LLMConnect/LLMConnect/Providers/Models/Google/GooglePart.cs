using System.Text.Json.Serialization;

namespace LLMConnect;

internal class GooglePart
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("functionCall")]
    public GoogleFunctionCall? FunctionCall { get; set; }
}