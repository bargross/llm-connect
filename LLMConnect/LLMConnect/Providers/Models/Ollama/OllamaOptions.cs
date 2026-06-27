using System.Text.Json.Serialization;

namespace LLMConnect;

internal class OllamaOptions
{
    [JsonPropertyName("temperature")]
    public float? Temperature { get; set; }

    [JsonPropertyName("top_p")]
    public float? TopP { get; set; }

    [JsonPropertyName("num_predict")]
    public int? NumPredict { get; set; }

    [JsonPropertyName("stop")]
    public List<string>? Stop { get; set; }

    [JsonPropertyName("frequency_penalty")]
    public float? FrequencyPenalty { get; set; }

    [JsonPropertyName("presence_penalty")]
    public float? PresencePenalty { get; set; }

    [JsonPropertyName("seed")]
    public int? Seed { get; set; }
}