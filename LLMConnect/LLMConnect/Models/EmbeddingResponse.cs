namespace LLMConnect.Models;

public class EmbeddingResponse
{
    public float[] Embedding { get; set; } = Array.Empty<float>();
    public string? Model { get; set; }
    public Usage? Usage { get; set; }
    public DateTime CreatedAt { get; set; }
}