namespace LLMConnect.Models;

/// <summary>
/// 
/// </summary>
public class AssistantMessage : Message 
{ 
    /// <summary>
    /// 
    /// </summary>
    /// <param name="content"></param>
    public AssistantMessage(string content) 
        : base(MessageRole.Assistant, content) 
    { 
    } 
}