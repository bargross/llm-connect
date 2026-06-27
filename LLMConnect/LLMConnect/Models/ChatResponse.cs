namespace LLMConnect.Models;

public class ChatResponse
{
    public string Content { get; set; }

    public string? FinishReason { get; set; }

    public Usage Usage { get; set; } = new();

    public string? Model { get; set; }

    public DateTime CreatedAt { get; set; }
}