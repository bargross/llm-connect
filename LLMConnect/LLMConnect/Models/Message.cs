using System.Text.Json.Serialization;

namespace LLMConnect.Models;

/// <summary>
/// The base type for a single message in a chat conversation. Use one of the
/// concrete subclasses (<see cref="SystemMessage"/>, <see cref="UserMessage"/>,
/// <see cref="AssistantMessage"/>, <see cref="ToolMessage"/>) rather than
/// constructing this type directly.
/// </summary>
public abstract class Message
{
    /// <summary>The role of the speaker who produced this message.</summary>
    [JsonPropertyName("role")]
    public MessageRole Role { get; protected set; }

    /// <summary>The text content of the message.</summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>
    /// Initializes a new message with the specified role and content.
    /// </summary>
    /// <param name="role">The role of the speaker who produced this message.</param>
    /// <param name="content">The text content of the message.</param>
    [JsonConstructor] 
    public Message(MessageRole role, string content)
    {
        Role = role;
        Content = content;
    }

    /// <summary>
    /// Initializes a new, empty message. Intended for deserialization scenarios;
    /// prefer <see cref="Message(MessageRole, string)"/> or a concrete subclass
    /// when constructing messages directly.
    /// </summary>
    public Message() { }
}

