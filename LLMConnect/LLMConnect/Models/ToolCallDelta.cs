namespace LLMConnect.Models;

/// <summary>
/// Represents the delta of a tool call in a sequence of tool calls.
/// </summary>
public class ToolCallDelta
{
    /// <summary>
    /// The index of the tool call in the sequence of tool calls.
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// The unique identifier of the tool call.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// The name of the tool being called.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The delta of the arguments passed to the tool call.
    /// </summary>
    public string? ArgumentsDelta { get; set; }
}