namespace LLMConnect.Models;

/// <summary>
/// Token usage statistics for a chat completion request.
/// </summary>
public class Usage
{

    /// <summary>The number of tokens in the input (prompt).</summary>
    public int InputTokens { get; set; }

    /// <summary>The number of tokens in the generated output.</summary>
    public int OutputTokens { get; set; }

    /// <summary>The total number of tokens used, computed as <see cref="InputTokens"/> + <see cref="OutputTokens"/>.</summary>
    public int TotalTokens => InputTokens + OutputTokens;
}
