using System.Text.Json.Serialization;

namespace LLMConnect;

internal class OpenAIChatRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = "gpt-3.5-turbo";

    [JsonPropertyName("messages")]
    public List<OpenAIMessage> Messages { get; set; } = new();

    [JsonPropertyName("temperature")]
    public float? Temperature { get; set; }

    [JsonPropertyName("top_p")]
    public float? TopP { get; set; }

    [JsonPropertyName("max_tokens")]
    public int? MaxTokens { get; set; }

    [JsonPropertyName("stream")]
    public bool Stream { get; set; } = false;

    [JsonPropertyName("stop")]
    public List<string>? Stop { get; set; }

    [JsonPropertyName("frequency_penalty")]
    public float? FrequencyPenalty { get; set; }

    [JsonPropertyName("presence_penalty")]
    public float? PresencePenalty { get; set; }

    [JsonPropertyName("response_format")]
    public OpenAIResponseFormat? ResponseFormat { get; set; }

    [JsonPropertyName("seed")]
    public int? Seed { get; set; }

    [JsonPropertyName("user")]
    public string? User { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object>? ExtraData { get; set; }
}