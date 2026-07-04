namespace LLMConnect.Models;

/// <summary>
/// Represents a request for generating embeddings from text using various providers (e.g., OpenAI, Google).
/// </summary>
public class EmbeddingRequest
{
    /// <summary>
    /// Text to embed. For the provider, this can be a string or an array of strings. For Google, this is a single string.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// model name for the embedding request (OpenAI, Google, etc.).
    /// </summary>
    public string? Model { get; set; }

    /// <summary>Optional user identifier for abuse monitoring (OpenAI).</summary>
    public string? User { get; set; }

    /// <summary>Optional encoding format; "float" (default) or "base64" (OpenAI).</summary>
    public string? EncodingFormat { get; set; }

    /// <summary>Optional number of dimensions for the embedding (OpenAI).</summary>
    public int? Dimensions { get; set; }

    /// <summary>Optional task type for Google: "RETRIEVAL_DOCUMENT", "RETRIEVAL_QUERY", etc.</summary>
    public string? TaskType { get; set; }

    /// <summary>Optional title for Google document embeddings.</summary>
    public string? Title { get; set; }

    /// <summary>Optional role for Google content (e.g., "user" or "model").</summary>
    public string? Role { get; set; }

    /// <summary>Provider-specific extra parameters (escape hatch).</summary>
    public Dictionary<string, object>? ExtraParameters { get; set; }
}