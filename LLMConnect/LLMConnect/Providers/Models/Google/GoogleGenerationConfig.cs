using System.Text.Json.Serialization;

namespace LLMConnect;

internal class GoogleGenerationConfig
{
    [JsonPropertyName("temperature")]
    public float? Temperature { get; set; }

    [JsonPropertyName("topP")]
    public float? TopP { get; set; }

    [JsonPropertyName("maxOutputTokens")]
    public int? MaxOutputTokens { get; set; }
}