namespace LLMConnect.Models;

/// <summary>
/// Represents a single incremental piece of a streamed chat completion,
/// as yielded by <see cref="ILLMConnectClient.StreamAsync"/>.
/// </summary>
public class ChatChunk
{
    /// <summary>The text content of this chunk, if any.</summary>
    public string? Content { get; set; }

    /// <summary><see langword="true"/> if this is the final chunk of the stream.</summary>
    public bool IsComplete { get; set; }

    /// <summary>
    /// The reason generation stopped, populated only on the final chunk where
    /// the provider's wire format makes it available.
    /// </summary>
    public string? FinishReason { get; set; }
}
