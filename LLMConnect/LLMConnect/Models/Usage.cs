namespace LLMConnect.Models;

public class Usage
{
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int TotalTokens => InputTokens + OutputTokens;
}
