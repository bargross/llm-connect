namespace LLMConnect.Models;

/// <summary>
/// 
/// </summary>
public class Usage
{

    /// <summary>
    /// 
    /// </summary>
    public int InputTokens { get; set; }
    
    /// <summary>
    /// 
    /// </summary>
    public int OutputTokens { get; set; }
    
    /// <summary>
    /// 
    /// </summary>
    public int TotalTokens => InputTokens + OutputTokens;
}
