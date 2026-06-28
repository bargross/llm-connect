using System.Text.Json.Serialization;

namespace LLMConnect.Models;

public abstract class Message
{
    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("role")]
    public string Role { get; protected set; }

    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="role"></param>
    /// <param name="content"></param>
    [JsonConstructor] 
    public Message(string role, string content)
    {
        Role = role;
        Content = content;
    }
}

