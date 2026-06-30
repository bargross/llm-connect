namespace LLMConnect.Models;

/// <summary>
/// A user-role message representing input from the end user.
/// </summary>
public class UserMessage : Message 
{
    /// <summary>
    /// Initializes a new user message with the specified content.
    /// </summary>
    /// <param name="content">The user's message text.</param>
    public UserMessage(string content) 
        : base(MessageRole.User, content) 
    { 
    } 
}
