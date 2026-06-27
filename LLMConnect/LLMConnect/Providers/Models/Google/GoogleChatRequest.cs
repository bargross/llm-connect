using System.Text.Json.Serialization;

namespace LLMConnect;

internal class GoogleChatRequest
{
    [JsonPropertyName("contents")]
    public List<GoogleContent> Contents { get; set; } = new();

    [JsonPropertyName("systemInstruction")]
    public GoogleContent? SystemInstruction { get; set; }

    [JsonPropertyName("generationConfig")]
    public GoogleGenerationConfig? GenerationConfig { get; set; }
}