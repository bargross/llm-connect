using System.Text.Json.Serialization;

namespace LLMConnect;

internal class OpenAIChatResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("created")]
    public long? Created { get; set; }

    [JsonPropertyName("choices")]
    public List<OpenAIChoice>? Choices { get; set; }

    [JsonPropertyName("usage")]
    public OpenAiUsage? Usage { get; set; }
}