using System.Text.Json.Serialization;

internal class OpenAIResponseFormat
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "text";
}