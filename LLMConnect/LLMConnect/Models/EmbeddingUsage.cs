

namespace LLMConnect.Models;

/// <summary>
/// Represents the usage of tokens in an embedding request and response.
/// </summary>
public class EmbeddingUsage
{
    /// <summary>
    /// Gets or sets the number of input tokens used in the embedding request.
    /// </summary>
    public int InputTokens { get; set; }

    /// <summary>
    /// Gets or sets the number of output tokens generated in the embedding response.
    /// </summary>
    public int OutputTokens { get; set; }

    /// <summary>
    /// Gets or sets the total number of tokens used in the embedding request and response.
    /// </summary>
    public int TotalTokens { get; set; }
}
