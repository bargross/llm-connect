using System.Text.Json.Serialization;

namespace LLMConnect;

internal class OpenAIStreamFunctionCall
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("arguments")]
    public string? Arguments { get; set; }
}
