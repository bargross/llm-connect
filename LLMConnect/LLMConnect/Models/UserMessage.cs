namespace LLMConnect.Models;

/// <summary>
/// 
/// </summary>
public class UserMessage : Message 
{ 
    /// <summary>
    /// 
    /// </summary>
    /// <param name="content"></param>
    public UserMessage(string content) 
        : base(MessageRole.User, content) 
    { 
    } 
}
