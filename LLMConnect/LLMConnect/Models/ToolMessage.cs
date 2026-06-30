namespace LLMConnect.Models;

/// <summary>
/// A tool-role message representing the result of a tool/function call,
/// returned to the model as part of the conversation history.
/// </summary>
public class ToolMessage : Message 
{
    /// <summary>The identifier of the tool call this message is a result for.</summary>
    public string ToolCallId { get; set; }

    /// <summary>
    /// Initializes a new tool result message.
    /// </summary>
    /// <param name="toolCallId">The identifier of the tool call this message responds to.</param>
    /// <param name="content">The tool's result content, typically serialized JSON or plain text.</param>
    public ToolMessage(string toolCallId, string content) 
        : base(MessageRole.Tool, content) 
    { 
        ToolCallId = toolCallId; 
    } 
}