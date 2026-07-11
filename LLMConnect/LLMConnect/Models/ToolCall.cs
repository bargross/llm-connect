namespace LLMConnect.Models;

/// <summary>
/// Represents a call to a tool with its identifier, name, and arguments.
/// </summary>
public class ToolCall
{
    /// <summary>
    /// Gets or sets the unique identifier of the tool call.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the tool being called.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the arguments passed to the tool call as a dictionary of key-value pairs.
    /// </summary>
    public Dictionary<string, object> Arguments { get; set; } = new();
}