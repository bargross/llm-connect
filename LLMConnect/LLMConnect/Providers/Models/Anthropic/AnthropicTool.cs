using System.Text.Json.Serialization;

namespace LLMConnect.Models;

internal class AnthropicTool
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("input_schema")]
    public Dictionary<string, object> InputSchema { get; set; } = new();
}