using System.Text.Json.Serialization;

namespace LLMConnect;

internal class OpenAIStreamChoice
{
    [JsonPropertyName("delta")]
    public OpenAIStreamDelta? Delta { get; set; }

    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }
}