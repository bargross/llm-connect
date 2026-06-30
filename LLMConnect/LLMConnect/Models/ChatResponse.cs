namespace LLMConnect.Models;

/// <summary>
/// Represents the complete result of a non-streaming chat completion request.
/// </summary>
public class ChatResponse
{
    /// <summary>The generated response text.</summary>
    public string? Content { get; set; }

    /// <summary>The reason generation stopped (e.g. <c>"stop"</c>, <c>"length"</c>, <c>"end_turn"</c>), as reported by the provider.</summary>
    public string? FinishReason { get; set; }

    /// <summary>Token usage statistics for this request and response.</summary>
    public Usage Usage { get; set; } = new();

    /// <summary>The model that generated this response.</summary>
    public string? Model { get; set; }

    /// <summary>The timestamp at which the response was created.</summary>
    public DateTime CreatedAt { get; set; }
}