using System.Text.Json.Serialization;

namespace LLMConnect;

internal class GooglePart
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}