namespace LLMConnect.Models;

public class SystemMessage : Message 
{ 
    public SystemMessage(string content) 
    { 
        Role = "system"; 
        Content = content; 
    } 
}