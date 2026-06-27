using System.Text.Json.Serialization;

namespace LLMConnect;

internal class GoogleContent
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("parts")]
    public List<GooglePart> Parts { get; set; } = new();
}