namespace LLMConnect.Models;

public class AssistantMessage : Message 
{ 
    public AssistantMessage(string content) 
    { 
        Role = "assistant"; 
        Content = content; 
    } 
}