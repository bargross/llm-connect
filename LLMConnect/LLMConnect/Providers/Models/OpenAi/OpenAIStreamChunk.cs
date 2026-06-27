using System.Text.Json.Serialization;

namespace LLMConnect;

internal class OpenAiStreamChunk
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("choices")]
    public List<OpenAIStreamChoice>? Choices { get; set; }
}