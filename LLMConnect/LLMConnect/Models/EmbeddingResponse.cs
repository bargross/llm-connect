namespace LLMConnect.Models;

/// <summary>
/// Represents the response from an embedding request to a language model provider.
/// </summary>
public class EmbeddingResponse
{
    /// <summary>
    /// Gets or sets the embedding vector returned by the language model provider.
    /// </summary>
    public float[] Embedding { get; set; } = Array.Empty<float>();

    /// <summary>
    /// Gets or sets the name of the model used to generate the embedding.
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// Gets or sets the usage information for the embedding request, if available.
    /// </summary>
    public EmbeddingUsage? Usage { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the embedding was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}