namespace LLMConnect.Models;

/// <summary>
/// A system-role message that sets the assistant's behavior, persona, or
/// constraints for the conversation.
/// </summary>
public class SystemMessage : Message 
{
    /// <summary>
    /// Initializes a new system message with the specified instruction text.
    /// </summary>
    /// <param name="content">The system instruction.</param>
    public SystemMessage(string content) 
        : base(MessageRole.System, content) 
    { 
    } 
}