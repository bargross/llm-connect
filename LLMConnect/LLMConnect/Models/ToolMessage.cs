namespace LLMConnect.Models;

/// <summary>
/// 
/// </summary>
public class ToolMessage : Message 
{ 
    /// <summary>
    /// 
    /// </summary>
    public string ToolCallId { get; set; } 
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="toolCallId"></param>
    /// <param name="content"></param>
    public ToolMessage(string toolCallId, string content) 
        : base("tool", content) 
    { 
        ToolCallId = toolCallId; 
    } 
}