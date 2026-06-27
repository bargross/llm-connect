using System;
using System.Collections.Generic;
using System.Text;

namespace LLMConnect.Models;

public class UserMessage : Message 
{ 
    public UserMessage(string content) 
    { 
        Role = "user"; 
        Content = content; 
    } 
}
