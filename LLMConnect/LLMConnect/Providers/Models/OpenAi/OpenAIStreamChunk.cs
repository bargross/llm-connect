using System.Text.Json.Serialization;

namespace LLMConnect;

internal class OpenAIStreamChunk
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("choices")]
    public List<OpenAIStreamChoice>? Choices { get; set; }
}