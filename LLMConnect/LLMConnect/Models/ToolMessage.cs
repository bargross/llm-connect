namespace LLMConnect.Models;

public class ToolMessage : Message 
{ 
    public string ToolCallId { get; set; } 
    public ToolMessage(string toolCallId, string content) 
    { 
        Role = "tool"; 
        ToolCallId = toolCallId; 
        Content = content; 
    } 
}