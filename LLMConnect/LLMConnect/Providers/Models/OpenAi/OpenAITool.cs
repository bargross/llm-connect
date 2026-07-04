using System.Text.Json.Serialization;

namespace LLMConnect.Models;

internal class OpenAITool
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";

    [JsonPropertyName("function")]
    public OpenAIFunction Function { get; set; } = new();
}