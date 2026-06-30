namespace LLMConnect.Models;

/// <summary>
/// An assistant-role message, typically representing prior model output
/// included as conversation history.
/// </summary>
public class AssistantMessage : Message 
{
    /// <summary>
    /// Initializes a new assistant message with the specified content.
    /// </summary>
    /// <param name="content">The assistant's message text.</param>
    public AssistantMessage(string content) 
        : base(MessageRole.Assistant, content) 
    { 
    } 
}