using System.Text.Json.Serialization;

namespace LLMConnect;

internal class AnthropicChatRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public List<AnthropicMessage> Messages { get; set; } = new();

    [JsonPropertyName("system")]
    public string? System { get; set; }

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; }

    [JsonPropertyName("temperature")]
    public float? Temperature { get; set; }

    [JsonPropertyName("top_p")]
    public float? TopP { get; set; }

    [JsonPropertyName("stream")]
    public bool? Stream { get; set; }

    [JsonPropertyName("stop_sequences")]
    public List<string>? StopSequences { get; set; }
}