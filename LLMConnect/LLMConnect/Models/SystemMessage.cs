namespace LLMConnect.Models;

/// <summary>
/// 
/// </summary>
public class SystemMessage : Message 
{ 
    /// <summary>
    /// 
    /// </summary>
    /// <param name="content"></param>
    public SystemMessage(string content) 
        : base("system", content) 
    { 
    } 
}