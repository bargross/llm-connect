using System.Text.Json.Serialization;

namespace LLMConnect.Models;

public abstract class Message
{
    [JsonPropertyName("role")]
    public string Role { get; protected set; }

    [JsonPropertyName("content")]
    public string Content { get; set; }

    [JsonConstructor]
    protected Message(string role, string content)
    {
        Role = role;
        Content = content;
    }
}

