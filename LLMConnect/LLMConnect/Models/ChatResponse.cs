namespace LLMConnect.Models;

/// <summary>
/// 
/// </summary>
public class ChatResponse
{
    /// <summary>
    /// 
    /// </summary>
    public string Content { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public string? FinishReason { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public Usage Usage { get; set; } = new();

    /// <summary>
    /// 
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public DateTime CreatedAt { get; set; }
}